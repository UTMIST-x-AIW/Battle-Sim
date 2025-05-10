using UnityEngine;
using System.IO;
using System;
using NEAT.Genes;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class SerializedNode
{
    public int key;
    public int type; // 0 = Input, 1 = Hidden, 2 = Output
    public int layer;
    public double bias;
}

[Serializable]
public class SerializedConnection
{
    public int key;
    public int inputKey;
    public int outputKey;
    public double weight;
    public bool enabled;
}

[Serializable]
public class SerializedBrain
{
    public SerializedNode[] nodes;
    public SerializedConnection[] connections;
}

[Serializable]
public class SavedCreature
{
    public Creature.CreatureType type;
    public float health;
    public float maxHealth;
    public float energyMeter;
    public float maxEnergy;
    public float lifetime;
    public int generation;
    public float moveSpeed;
    public float pushForce;
    public float visionRange;
    public float chopRange;
    public float actionEnergyCost;
    public float chopDamage;
    public float attackDamage;
    public float weightMutationRate;
    public float mutationRange;
    public float addNodeRate;
    public float addConnectionRate;
    public float deleteConnectionRate;
    public int maxHiddenLayers;
    public SerializedBrain brain;
}

public static class CreatureSaver
{
    private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "SavedCreatures");
    
    private static void EnsureSaveDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
                Debug.Log($"Created save directory at: {SaveDirectory}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create save directory: {e.Message}");
            throw; // Re-throw the exception to be handled by the caller
        }
    }
    
    private static SerializedBrain SerializeBrain(NEAT.NN.FeedForwardNetwork brain)
    {
        if (brain == null) return null;
        
        // Use public methods instead of reflection - fixes build serialization issues
        var nodes = brain.GetNodes();
        var connections = brain.GetConnections();
        
        if (nodes == null || connections == null) return null;
        
        var serializedBrain = new SerializedBrain
        {
            nodes = nodes.Values.Select(n => new SerializedNode
            {
                key = n.Key,
                type = (int)n.Type,
                layer = n.Layer,
                bias = n.Bias
            }).ToArray(),
            
            connections = connections.Values.Select(c => new SerializedConnection
            {
                key = c.Key,
                inputKey = c.InputKey,
                outputKey = c.OutputKey,
                weight = c.Weight,
                enabled = c.Enabled
            }).ToArray()
        };
        
        return serializedBrain;
    }
    
    public static void SaveCreature(Creature creature)
    {
        if (creature == null) return;
        
        try
        {
            // Ensure save directory exists
            EnsureSaveDirectoryExists();
            
            var savedCreature = new SavedCreature
            {
                type = creature.type,
                health = creature.health,
                maxHealth = creature.maxHealth,
                energyMeter = creature.energyMeter,
                maxEnergy = creature.maxEnergy,
                lifetime = creature.Lifetime,
                generation = creature.generation,
                moveSpeed = creature.moveSpeed,
                pushForce = creature.pushForce,
                visionRange = creature.visionRange,
                chopRange = creature.chopRange,
                actionEnergyCost = creature.actionEnergyCost,
                chopDamage = creature.chopDamage,
                attackDamage = creature.attackDamage,
                weightMutationRate = creature.weightMutationRate,
                mutationRange = creature.mutationRange,
                addNodeRate = creature.addNodeRate,
                addConnectionRate = creature.addConnectionRate,
                deleteConnectionRate = creature.deleteConnectionRate,
                maxHiddenLayers = creature.maxHiddenLayers,
                brain = SerializeBrain(creature.GetBrain())
            };
            
            // Generate a unique filename based on creature type, generation, and timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{creature.type}_Gen{creature.generation}_{timestamp}.json";
            string filepath = Path.Combine(SaveDirectory, filename);
            
            // Convert to JSON and save
            string json = JsonUtility.ToJson(savedCreature, true);
            File.WriteAllText(filepath, json);
            
            Debug.Log($"Successfully saved creature to: {filepath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save creature: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
} 