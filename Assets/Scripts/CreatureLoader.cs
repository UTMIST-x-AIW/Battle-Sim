using UnityEngine;
using System.IO;
using System;
using NEAT.Genes;
using System.Collections.Generic;

public static class CreatureLoader
{
    public static Creature LoadCreature(GameObject prefab, Vector3 position, string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"Failed to load creature: File not found at {jsonPath}");
                return null;
            }

            string json = File.ReadAllText(jsonPath);
            SavedCreature savedCreature = JsonUtility.FromJson<SavedCreature>(json);

            // Create the creature instance
            var creature = GameObject.Instantiate(prefab, position, Quaternion.identity);
            var creatureComponent = creature.GetComponent<Creature>();

            // Load basic properties
            creatureComponent.type = savedCreature.type;
            creatureComponent.health = savedCreature.health;
            creatureComponent.maxHealth = savedCreature.maxHealth;
            creatureComponent.energyMeter = savedCreature.energyMeter;
            creatureComponent.maxEnergy = savedCreature.maxEnergy;
            creatureComponent.Lifetime = savedCreature.lifetime;
            creatureComponent.generation = savedCreature.generation;
            creatureComponent.moveSpeed = savedCreature.moveSpeed;
            creatureComponent.pushForce = savedCreature.pushForce;
            creatureComponent.visionRange = savedCreature.visionRange;
            creatureComponent.chopRange = savedCreature.chopRange;
            creatureComponent.bowRange = savedCreature.bowRange;
            creatureComponent.actionEnergyCost = savedCreature.actionEnergyCost;
            creatureComponent.chopDamage = savedCreature.chopDamage;
            creatureComponent.attackDamage = savedCreature.attackDamage;
            creatureComponent.weightMutationRate = savedCreature.weightMutationRate;
            creatureComponent.mutationRange = savedCreature.mutationRange;
            creatureComponent.addNodeRate = savedCreature.addNodeRate;
            creatureComponent.addConnectionRate = savedCreature.addConnectionRate;
            creatureComponent.deleteConnectionRate = savedCreature.deleteConnectionRate;
            creatureComponent.maxHiddenLayers = savedCreature.maxHiddenLayers;

            // Reconstruct the brain
            if (savedCreature.brain != null)
            {
                var nodes = new Dictionary<int, NodeGene>();
                var connections = new Dictionary<int, ConnectionGene>();

                // Reconstruct nodes
                foreach (var node in savedCreature.brain.nodes)
                {
                    var nodeGene = new NodeGene(node.key, (NodeType)node.type)
                    {
                        Layer = node.layer,
                        Bias = node.bias
                    };
                    nodes[node.key] = nodeGene;
                }

                // Reconstruct connections
                foreach (var conn in savedCreature.brain.connections)
                {
                    var connectionGene = new ConnectionGene(conn.key, conn.inputKey, conn.outputKey, conn.weight)
                    {
                        Enabled = conn.enabled
                    };
                    connections[conn.key] = connectionGene;
                }

                // Create and initialize the neural network
                var network = new NEAT.NN.FeedForwardNetwork(nodes, connections);
                creatureComponent.InitializeNetwork(network);
            }
            else
            {
                Debug.LogWarning("No brain data found in saved creature file");
            }

            Debug.Log($"Successfully loaded creature from: {jsonPath}");
            return creatureComponent;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load creature: {e.Message}\nStack trace: {e.StackTrace}");
            return null;
        }
    }
} 