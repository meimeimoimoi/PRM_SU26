#!/usr/bin/env python3
"""
SmartDine Robot Sidecar
=======================
Bridge between the Webots robot controller (file I/O) and the deployed map-server (HTTP)
with real-time SignalR connection for instant command delivery and state updates.

Startup:
  1. Fetch active map from server
  2. Download map files (map.pgm, graph.json, waypoints.txt, meta.json)
  3. Write them to the robot controller directory
  4. Connect to backend SignalR hub

Main loop (~200ms):
  1. Read robot_state.txt → push via SignalR (or HTTP fallback)
  2. Read robot_path.txt → push via SignalR (or HTTP fallback)
  3. GET /api/robot/command → write command.txt (or receive via SignalR push)

Usage:
  pip install -r requirements.txt
  python robot_sidecar.py [--server URL] [--dir CONTROLLER_DIR] [--interval SECONDS]
"""

import argparse
import base64
import hashlib
import json
import logging
import os
import signal
import sys
import time

import requests
from signalrcore.hub.base_hub_connection import HubConnectionBuilder
from signalrcore.services.base_reconnect_service import BaseReconnectService

# ---------------------------------------------------------------------------
# Logging
# ---------------------------------------------------------------------------
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger("sidecar")

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------
DEFAULT_SERVER_URL = os.environ.get("MAP_SERVER_URL", "http://localhost:3001")
DEFAULT_CONTROLLER_DIR = os.environ.get(
    "CONTROLLER_DIR",
    os.path.join(os.path.dirname(__file__), "..", "controllers", "robot_controller"),
)
DEFAULT_POLL_INTERVAL = float(os.environ.get("POLL_INTERVAL", "0.2"))
DEFAULT_MAP_ID = os.environ.get("MAP_ID", None)  # None = auto-detect from server

# ---------------------------------------------------------------------------
# File helpers
# ---------------------------------------------------------------------------

def read_file(filepath: str) -> str | None:
    """Read a text file, return None if not found or empty."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            return f.read().strip()
    except FileNotFoundError:
        return None
    except Exception as e:
        log.warning(f"Error reading {filepath}: {e}")
        return None


def write_file(filepath: str, content: str) -> bool:
    """Write content to a file atomically (write tmp then rename)."""
    tmp = filepath + ".tmp"
    try:
        with open(tmp, "w", encoding="utf-8") as f:
            f.write(content)
        # Atomic replace
        if os.path.exists(filepath):
            os.remove(filepath)
        os.rename(tmp, filepath)
        return True
    except Exception as e:
        log.error(f"Error writing {filepath}: {e}")
        return False


def file_hash(filepath: str) -> str:
    """Return MD5 hash of file content for change detection."""
    try:
        with open(filepath, "rb") as f:
            return hashlib.md5(f.read()).hexdigest()
    except FileNotFoundError:
        return ""


# ---------------------------------------------------------------------------
# Robot state/path parsers (match robot_controller.c output format)
# ---------------------------------------------------------------------------

def parse_robot_state(raw: str) -> dict | None:
    """
    Parse robot_state.txt format: 'x y theta v omega STATUS'
    Returns dict or None if invalid.
    """
    parts = raw.split()
    if len(parts) < 6:
        return None
    try:
        return {
            "x": float(parts[0]),
            "y": float(parts[1]),
            "theta": float(parts[2]),
            "v": float(parts[3]),
            "omega": float(parts[4]),
            "status": parts[5],
        }
    except (ValueError, IndexError):
        return None


def parse_robot_path(raw: str) -> list[dict]:
    """
    Parse robot_path.txt format: multi-line 'x y' pairs, or 'NONE'.
    Returns list of {x, y} dicts.
    """
    if not raw or raw.strip() == "NONE":
        return []
    points = []
    for line in raw.strip().split("\n"):
        parts = line.strip().split()
        if len(parts) >= 2:
            try:
                x, y = float(parts[0]), float(parts[1])
                if _is_finite(x) and _is_finite(y):
                    points.append({"x": x, "y": y})
            except ValueError:
                continue
    return points


def _is_finite(v: float) -> bool:
    return v == v and abs(v) != float("inf")


# ---------------------------------------------------------------------------
# Server communication
# ---------------------------------------------------------------------------

class ServerClient:
    """HTTP client for the map-server."""

    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip("/")
        self.session = requests.Session()
        self.session.headers.update({"Content-Type": "application/json"})

    def _url(self, path: str) -> str:
        return f"{self.base_url}{path}"

    def is_alive(self) -> bool:
        try:
            r = self.session.get(self._url("/api/robot/command"), timeout=3)
            return r.status_code == 200
        except Exception:
            return False

    def list_maps(self) -> list[dict] | None:
        try:
            r = self.session.get(self._url("/api/maps"), timeout=5)
            r.raise_for_status()
            return r.json()
        except Exception as e:
            log.error(f"Failed to list maps: {e}")
            return None

    def get_map_files(self, map_id: str) -> dict | None:
        """Download all map files for a given map ID."""
        try:
            r = self.session.get(self._url(f"/api/robot/map-files/{map_id}"), timeout=30)
            r.raise_for_status()
            return r.json()
        except Exception as e:
            log.error(f"Failed to get map files for {map_id}: {e}")
            return None

    def post_robot_state(self, state: dict) -> bool:
        try:
            r = self.session.post(self._url("/api/robot/state"), json=state, timeout=3)
            r.raise_for_status()
            return True
        except Exception as e:
            log.warning(f"Failed to post robot state: {e}")
            return False

    def post_robot_path(self, path: list[dict]) -> bool:
        try:
            r = self.session.post(self._url("/api/robot/path"), json={"path": path}, timeout=3)
            r.raise_for_status()
            return True
        except Exception as e:
            log.warning(f"Failed to post robot path: {e}")
            return False

    def get_command(self) -> dict | None:
        try:
            r = self.session.get(self._url("/api/robot/command"), timeout=3)
            r.raise_for_status()
            return r.json()
        except Exception as e:
            log.warning(f"Failed to get command: {e}")
            return None


# ---------------------------------------------------------------------------
# Map file writer (sidecar → controller dir)
# ---------------------------------------------------------------------------

def write_map_files_to_controller(controller_dir: str, map_data: dict) -> bool:
    """Write downloaded map files into the robot controller directory."""
    os.makedirs(controller_dir, exist_ok=True)

    wrote = 0

    # meta.json
    if "meta" in map_data:
        p = os.path.join(controller_dir, "map_meta.json")
        write_file(p, json.dumps(map_data["meta"], indent=2))
        wrote += 1

    # graph.json
    if "graph" in map_data:
        p = os.path.join(controller_dir, "graph.json")
        write_file(p, json.dumps(map_data["graph"], indent=2))
        wrote += 1

    # waypoints.txt
    if "waypoints" in map_data:
        p = os.path.join(controller_dir, "waypoints.txt")
        write_file(p, map_data["waypoints"])
        wrote += 1

    # map.yaml
    if "mapYaml" in map_data:
        p = os.path.join(controller_dir, "map.yaml")
        write_file(p, map_data["mapYaml"])
        wrote += 1

    # map.pgm (base64 → binary)
    if "mapPgmBase64" in map_data:
        p = os.path.join(controller_dir, "map.pgm")
        try:
            pgm_bytes = base64.b64decode(map_data["mapPgmBase64"])
            with open(p, "wb") as f:
                f.write(pgm_bytes)
            wrote += 1
        except Exception as e:
            log.error(f"Failed to decode/write map.pgm: {e}")

    log.info(f"Wrote {wrote}/5 map files to {controller_dir}")
    return wrote > 0


# ---------------------------------------------------------------------------
# Main sidecar class
# ---------------------------------------------------------------------------

class RobotSidecar:
    def __init__(self, server_url: str, controller_dir: str, poll_interval: float, map_id: str | None):
        self.client = ServerClient(server_url)
        self.controller_dir = os.path.abspath(controller_dir)
        self.poll_interval = poll_interval
        self.map_id = map_id
        self.running = True

        # Change detection hashes
        self._last_state_hash = ""
        self._last_path_hash = ""
        self._last_command = None

        # SignalR connection
        self.signalr_conn = None

    def startup(self) -> bool:
        """Connect to server and download map files."""
        log.info(f"Connecting to server: {self.client.base_url}")
        if not self.client.is_alive():
            log.error("Server is not reachable! Will retry in main loop.")
            return False

        # Resolve map ID
        if not self.map_id:
            maps = self.client.list_maps()
            if not maps:
                log.error("No maps found on server!")
                return False
            # Pick the most recent map
            maps.sort(key=lambda m: m.get("createdAt", ""), reverse=True)
            self.map_id = maps[0]["id"]
            log.info(f"Auto-selected map: {self.map_id}")

        # Download map files
        map_data = self.client.get_map_files(self.map_id)
        if not map_data:
            log.error(f"Failed to download map {self.map_id}")
            return False

        # Write to controller dir
        write_map_files_to_controller(self.controller_dir, map_data)
        log.info(f"Map {self.map_id} loaded into {self.controller_dir}")

        # Connect to SignalR for real-time commands
        self._connect_signalr()

        return True

    def tick(self):
        """One iteration of the main loop."""
        # When SignalR is connected, state/path are pushed via SignalR.
        # Only fall back to HTTP polling when SignalR is not available.
        if self.signalr_conn is None:
            self._sync_state()
            self._sync_path()
        self._sync_command()

    def _sync_state(self):
        """Read robot_state.txt → POST to server (only if changed)."""
        state_file = os.path.join(self.controller_dir, "robot_state.txt")
        h = file_hash(state_file)
        if h == self._last_state_hash:
            return  # No change
        self._last_state_hash = h

        raw = read_file(state_file)
        if not raw:
            return

        state = parse_robot_state(raw)
        if state:
            self.client.post_robot_state(state)

    def _sync_path(self):
        """Read robot_path.txt → POST to server (only if changed)."""
        path_file = os.path.join(self.controller_dir, "robot_path.txt")
        h = file_hash(path_file)
        if h == self._last_path_hash:
            return  # No change
        self._last_path_hash = h

        raw = read_file(path_file)
        if raw is None:
            return

        points = parse_robot_path(raw)
        self.client.post_robot_path(points)

    def _sync_command(self):
        """GET command from server → write command.txt (only if new command)."""
        cmd = self.client.get_command()
        if not cmd:
            return

        cmd_str = f"{cmd.get('command', 'NONE')} {cmd.get('target', 'NONE')} {cmd.get('direction', 'NONE')}"

        # Skip if same as last sent or if NONE
        if cmd_str == self._last_command:
            return
        if cmd.get("command") == "NONE" and self._last_command is not None:
            return  # Don't write NONE if we already wrote a real command

        self._last_command = cmd_str
        command_file = os.path.join(self.controller_dir, "command.txt")
        write_file(command_file, cmd_str)
        log.info(f"Command written: {cmd_str}")

    # ------------------------------------------------------------------
    # SignalR connection
    # ------------------------------------------------------------------

    def _connect_signalr(self):
        """Connect to backend SignalR hub for real-time commands."""
        signalr_url = os.environ.get("SIGNALR_URL", "http://localhost:5000/hubs/robot")
        token = os.environ.get("AUTH_TOKEN", "")

        self.signalr_conn = (
            HubConnectionBuilder()
            .with_url(signalr_url, options={"access_token_factory": lambda: token})
            .with_automatic_reconnect(
                BaseReconnectService(max_reconnect_attempts=10, reconnect_interval=5)
            )
            .build()
        )

        self.signalr_conn.on_open(lambda: self._on_signalr_connected())
        self.signalr_conn.on_close(lambda: self._on_signalr_disconnected())
        self.signalr_conn.on("ReceiveRobotCommand", self._on_robot_command)

        try:
            self.signalr_conn.start()
            log.info(f"SignalR connected to {signalr_url}")
        except Exception as e:
            log.error(f"SignalR connection failed: {e}")

    def _on_signalr_connected(self):
        log.info("SignalR connected, joining RobotGroup")
        try:
            self.signalr_conn.invoke("JoinRobotGroup")
        except Exception as e:
            log.error(f"Failed to join RobotGroup: {e}")

    def _on_signalr_disconnected(self):
        log.warning("SignalR disconnected")

    def _on_robot_command(self, args):
        """Handle command received via SignalR."""
        if args and len(args) > 0:
            cmd = args[0]
            cmd_str = f"{cmd.get('command', 'NONE')} {cmd.get('target', 'NONE')} {cmd.get('direction', 'NONE')}"
            command_file = os.path.join(self.controller_dir, "command.txt")
            write_file(command_file, cmd_str)
            log.info(f"SignalR command received: {cmd_str}")

    def _sync_state_signalr(self):
        """Push robot state via SignalR (replaces HTTP POST)."""
        state_file = os.path.join(self.controller_dir, "robot_state.txt")
        h = file_hash(state_file)
        if h == self._last_state_hash:
            return
        self._last_state_hash = h
        raw = read_file(state_file)
        if not raw:
            return
        state = parse_robot_state(raw)
        if state and self.signalr_conn:
            try:
                self.signalr_conn.invoke(
                    "SendRobotState",
                    state["x"],
                    state["y"],
                    state["theta"],
                    state["v"],
                    state["omega"],
                    state["status"],
                )
            except Exception as e:
                log.warning(f"SignalR state push failed: {e}")

    def _sync_path_signalr(self):
        """Push robot path via SignalR (replaces HTTP POST)."""
        path_file = os.path.join(self.controller_dir, "robot_path.txt")
        h = file_hash(path_file)
        if h == self._last_path_hash:
            return
        self._last_path_hash = h
        raw = read_file(path_file)
        if raw is None:
            return
        points = parse_robot_path(raw)
        if self.signalr_conn:
            try:
                self.signalr_conn.invoke("SendRobotPath", points)
            except Exception as e:
                log.warning(f"SignalR path push failed: {e}")

    def run(self):
        """Main loop with graceful shutdown."""
        log.info(f"Sidecar started (poll={self.poll_interval}s, dir={self.controller_dir})")

        # Try startup, retry if server not ready
        connected = self.startup()
        if not connected:
            log.info("Waiting for server to become available...")

        while self.running:
            try:
                if not connected:
                    connected = self.startup()
                else:
                    self.tick()
                    self._sync_state_signalr()
                    self._sync_path_signalr()
            except KeyboardInterrupt:
                break
            except Exception as e:
                log.error(f"Tick error: {e}")

            time.sleep(self.poll_interval)

        log.info("Sidecar stopped.")

    def stop(self):
        self.running = False


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="SmartDine Robot Sidecar")
    parser.add_argument("--server", default=DEFAULT_SERVER_URL, help="Map server URL")
    parser.add_argument("--dir", default=DEFAULT_CONTROLLER_DIR, help="Robot controller directory")
    parser.add_argument("--interval", type=float, default=DEFAULT_POLL_INTERVAL, help="Poll interval (seconds)")
    parser.add_argument("--map-id", default=DEFAULT_MAP_ID, help="Map ID to use (auto-detect if not set)")
    args = parser.parse_args()

    sidecar = RobotSidecar(
        server_url=args.server,
        controller_dir=args.dir,
        poll_interval=args.interval,
        map_id=args.map_id,
    )

    def shutdown(signum, frame):
        log.info("Shutdown signal received...")
        sidecar.stop()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    sidecar.run()


if __name__ == "__main__":
    main()
