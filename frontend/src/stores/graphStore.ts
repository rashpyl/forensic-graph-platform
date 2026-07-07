import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface GraphNode {
  id: string
  title: string
  description: string
  occurredAt: string // ISO datetime string
  type: 'crime_event'
}

export interface GraphEdge {
  id: string
  sourceId: string
  targetId: string
}

export const useGraphStore = defineStore('graph', () => {
  const nodes = ref<GraphNode[]>([])
  const edges = ref<GraphEdge[]>([])
  const selectedNodeId = ref<string | null>(null)
  const selectedEdgeId = ref<string | null>(null)

  function addNode(title: string, description: string, occurredAt: string): GraphNode {
    const node: GraphNode = {
      id: `node-${Date.now()}`,
      title,
      description,
      occurredAt,
      type: 'crime_event',
    }
    nodes.value.push(node)
    return node
  }

  function removeNode(id: string) {
    nodes.value = nodes.value.filter((n) => n.id !== id)
    edges.value = edges.value.filter((e) => e.sourceId !== id && e.targetId !== id)
    if (selectedNodeId.value === id) selectedNodeId.value = null
  }

  function removeEdge(id: string) {
    edges.value = edges.value.filter((e) => e.id !== id)
    if (selectedEdgeId.value === id) selectedEdgeId.value = null
  }

  function addEdge(sourceId: string, targetId: string): GraphEdge | null {
    const duplicate = edges.value.find(
      (e) =>
        (e.sourceId === sourceId && e.targetId === targetId) ||
        (e.sourceId === targetId && e.targetId === sourceId),
    )
    if (duplicate) return null

    const edge: GraphEdge = {
      id: `edge-${Date.now()}`,
      sourceId,
      targetId,
    }
    edges.value.push(edge)
    return edge
  }

  function selectNode(id: string | null) {
    selectedNodeId.value = id
    if (id) selectedEdgeId.value = null
  }

  function selectEdge(id: string | null) {
    selectedEdgeId.value = id
    if (id) selectedNodeId.value = null
  }

  function getNode(id: string): GraphNode | undefined {
    return nodes.value.find((n) => n.id === id)
  }

  function getEdge(id: string): GraphEdge | undefined {
    return edges.value.find((e) => e.id === id)
  }

  return {
    nodes,
    edges,
    selectedNodeId,
    selectedEdgeId,
    addNode,
    removeNode,
    removeEdge,
    addEdge,
    selectNode,
    selectEdge,
    getNode,
    getEdge,
  }
})
