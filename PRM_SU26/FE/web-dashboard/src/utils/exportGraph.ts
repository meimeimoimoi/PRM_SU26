// src/utils/exportGraph.ts
import type { GraphEdge } from '../types/map';
import type { GraphMapMeta, GraphNode } from '../types/graph';

export interface ExportedGraphData {
  meta: GraphMapMeta;
  nodes: GraphNode[];
  edges: GraphEdge[];
}

export function exportGraph(nodes: GraphNode[], edges: GraphEdge[], floorSize: number, resolution: number): string {
  const normalizedNodes = nodes.map((node) => ({
    ...node,
    name: node.type === 'delivery' && !node.name.toLowerCase().includes('delivery')
      ? `${node.name}_Delivery`
      : node.name,
  }));

  // Lấy tập hợp ID hợp lệ sau khi normalize
  const validNodeIds = new Set(normalizedNodes.map((n) => n.id));

  // Lọc bỏ dangling edges (from/to trỏ tới node không tồn tại)
  const validEdges = edges.filter((edge) => {
    const fromOk = validNodeIds.has(edge.from);
    const toOk = validNodeIds.has(edge.to);
    if (!fromOk || !toOk) {
      console.warn(`[exportGraph] Bỏ qua edge "${edge.id}": from="${edge.from}" (${fromOk ? 'ok' : 'MISSING'}) → to="${edge.to}" (${toOk ? 'ok' : 'MISSING'})`);
    }
    return fromOk && toOk;
  });

  const payload: ExportedGraphData = {
    meta: {
      version: 2,
      floorSize,
      resolution,
    },
    nodes: normalizedNodes,
    edges: validEdges,
  };

  return JSON.stringify(payload, null, 2);
}
