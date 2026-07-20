# Mermaid Diagrams for SmartDine (draw.io Compatible)

## 18. SmartDine System Overview (Section VI - User Manual §3.1)

```mermaid
graph TB
    subgraph SmartDine["SmartDine System"]
        direction LR
        subgraph CM["Customer Mobile (Flutter App)"]
            CM1["Browse Menu"]
            CM2["Place Order"]
            CM3["Pay Bill"]
            CM4["View History"]
            CM5["Loyalty Points"]
            CM6["QR Scanning"]
        end
        subgraph WD["Web Dashboard (React Admin)"]
            WD1["Manage Menu"]
            WD2["Manage Tables"]
            WD3["View Orders"]
            WD4["Manage Staff"]
            WD5["View Reports"]
            WD6["Robot Control"]
        end
        subgraph RC["Robot Console (Webots + Web)"]
            RC1["View Robot"]
            RC2["Send Commands"]
            RC3["Monitor Path"]
            RC4["Configure Map"]
        end
    end
```

---

## 19. Workflow Diagram - Customer Order Flow (Section VI §3.2)

```mermaid
flowchart LR
    A["Scan QR Code"] --> B["Browse Menu"]
    B --> C["Add to Cart"]
    C --> D["Place Order"]
    D --> E["Pay Bill"]
```

---

## 20. Workflow Diagram - Kitchen Processing Flow (Section VI §3.3)

```mermaid
flowchart LR
    A["View Orders"] --> B["Confirm Order"]
    B --> C["Cook Order"]
    C --> D["Mark Ready"]
```

---

## 21. Workflow Diagram - Manager Management Flow (Section VI §3.4)

```mermaid
flowchart LR
    A["Login"] --> B["Manage Menu"]
    B --> C["View Reports"]
    C --> D["Manage Staff"]
```

---

## 22. Workflow Diagram - Robot Delivery Flow (Section VI §3.5)

```mermaid
flowchart LR
    A["Staff Commands"] --> B["Robot Navigate"]
    B --> C["Robot Pick Up"]
    C --> D["Robot Deliver"]
    D --> E["Robot Return"]
```
