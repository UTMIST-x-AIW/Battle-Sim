using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NEAT.Genes;

[Serializable]
public class SavedInteractableState
{
    public string prefabName;
    public float hitPoints;
    public float currentHP;
    public Vector3 position;
}

[Serializable]
public class SavedCreatureState
{
    public SavedCreature creature;
    public Vector3 position;
}

[Serializable]
public class GameState
{
    public List<SavedCreatureState> creatures = new List<SavedCreatureState>();
    public List<SavedInteractableState> interactables = new List<SavedInteractableState>();
    public float timestamp;
}

public class GameStateManager : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject albertPrefab;
    public GameObject kaiPrefab;
    public GameObject treePrefab;
    public GameObject rockPrefab;
    public GameObject cupcakePrefab;

    [Header("Autosave Settings")]
    public float autosaveInterval = 1800f; // 30 minutes

    private float lastSaveTime;

    private string SaveDirectory => Path.Combine(Application.persistentDataPath, "GameStates");

    private void Start()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }
        lastSaveTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - lastSaveTime >= autosaveInterval)
        {
            SaveGameState();
            lastSaveTime = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            SaveGameState();
        }
        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadLatestGameState();
        }
    }

    private SerializedBrain SerializeBrain(NEAT.NN.FeedForwardNetwork brain)
    {
        if (brain == null) return null;

        var nodes = brain.GetNodes();
        var connections = brain.GetConnections();

        if (nodes == null || connections == null) return null;

        var serializedBrain = new SerializedBrain
        {
            nodes = new SerializedNode[nodes.Count],
            connections = new SerializedConnection[connections.Count]
        };

        int index = 0;
        foreach (var n in nodes.Values)
        {
            serializedBrain.nodes[index++] = new SerializedNode
            {
                key = n.Key,
                type = (int)n.Type,
                layer = n.Layer,
                bias = n.Bias
            };
        }

        index = 0;
        foreach (var c in connections.Values)
        {
            serializedBrain.connections[index++] = new SerializedConnection
            {
                key = c.Key,
                inputKey = c.InputKey,
                outputKey = c.OutputKey,
                weight = c.Weight,
                enabled = c.Enabled
            };
        }

        return serializedBrain;
    }

    private SavedCreature CreateSavedCreature(Creature creature)
    {
        return new SavedCreature
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
            closeRange = creature.closeRange,
            bowRange = creature.bowRange,
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
    }

    public void SaveGameState()
    {
        try
        {
            GameState state = new GameState();
            state.timestamp = Time.time;

            var creatures = FindObjectsOfType<Creature>();
            foreach (var c in creatures)
            {
                var savedC = CreateSavedCreature(c);
                state.creatures.Add(new SavedCreatureState
                {
                    creature = savedC,
                    position = c.transform.position
                });
            }

            var interactables = FindObjectsOfType<Interactable>();
            foreach (var i in interactables)
            {
                state.interactables.Add(new SavedInteractableState
                {
                    prefabName = i.gameObject.name.Replace("(Clone)", ""),
                    hitPoints = i.hitPoints,
                    currentHP = i.CurrentHP,
                    position = i.transform.position
                });
            }

            string json = JsonUtility.ToJson(state, true);
            string filename = "gamestate_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            string path = Path.Combine(SaveDirectory, filename);
            File.WriteAllText(path, json);
            Debug.Log("Game state saved to: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save game state: " + e.Message);
        }
    }

    private GameObject GetCreaturePrefab(Creature.CreatureType type)
    {
        return type == Creature.CreatureType.Albert ? albertPrefab : kaiPrefab;
    }

    private GameObject GetInteractablePrefab(string name)
    {
        switch (name)
        {
            case "Tree":
                return treePrefab;
            case "Rock":
                return rockPrefab;
            case "Cupcake":
                return cupcakePrefab;
            default:
                return null;
        }
    }

    public void LoadLatestGameState()
    {
        var files = Directory.GetFiles(SaveDirectory, "*.json");
        if (files.Length == 0)
        {
            Debug.LogWarning("No saved game states found");
            return;
        }
        Array.Sort(files);
        LoadGameState(files[files.Length - 1]);
    }

    public void LoadGameState(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Debug.LogError("Game state file not found: " + path);
                return;
            }

            string json = File.ReadAllText(path);
            GameState state = JsonUtility.FromJson<GameState>(json);

            foreach (var c in FindObjectsOfType<Creature>())
            {
                Destroy(c.gameObject);
            }
            foreach (var i in FindObjectsOfType<Interactable>())
            {
                Destroy(i.gameObject);
            }

            foreach (var inter in state.interactables)
            {
                var prefab = GetInteractablePrefab(inter.prefabName);
                if (prefab == null) continue;
                var go = Instantiate(prefab, inter.position, Quaternion.identity);
                var comp = go.GetComponent<Interactable>();
                comp.hitPoints = inter.hitPoints;
                comp.CurrentHP = inter.currentHP;
            }

            foreach (var cre in state.creatures)
            {
                var prefab = GetCreaturePrefab(cre.creature.type);
                var go = Instantiate(prefab, cre.position, Quaternion.identity);
                var comp = go.GetComponent<Creature>();

                var data = cre.creature;
                comp.type = data.type;
                comp.health = data.health;
                comp.maxHealth = data.maxHealth;
                comp.energyMeter = data.energyMeter;
                comp.maxEnergy = data.maxEnergy;
                comp.Lifetime = data.lifetime;
                comp.generation = data.generation;
                comp.moveSpeed = data.moveSpeed;
                comp.pushForce = data.pushForce;
                comp.closeRange = data.closeRange;
                comp.bowRange = data.bowRange;
                comp.actionEnergyCost = data.actionEnergyCost;
                comp.chopDamage = data.chopDamage;
                comp.attackDamage = data.attackDamage;
                comp.weightMutationRate = data.weightMutationRate;
                comp.mutationRange = data.mutationRange;
                comp.addNodeRate = data.addNodeRate;
                comp.addConnectionRate = data.addConnectionRate;
                comp.deleteConnectionRate = data.deleteConnectionRate;
                comp.maxHiddenLayers = data.maxHiddenLayers;

                if (data.brain != null)
                {
                    var nodes = new Dictionary<int, NodeGene>();
                    foreach (var node in data.brain.nodes)
                    {
                        var ng = new NodeGene(node.key, (NodeType)node.type)
                        {
                            Layer = node.layer,
                            Bias = node.bias
                        };
                        nodes[node.key] = ng;
                    }

                    var connections = new Dictionary<int, ConnectionGene>();
                    foreach (var conn in data.brain.connections)
                    {
                        var cg = new ConnectionGene(conn.key, conn.inputKey, conn.outputKey, conn.weight)
                        {
                            Enabled = conn.enabled
                        };
                        connections[conn.key] = cg;
                    }

                    var network = new NEAT.NN.FeedForwardNetwork(nodes, connections);
                    comp.InitializeNetwork(network);
                }
            }

            Debug.Log("Game state loaded from: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load game state: " + e.Message);
        }
    }
}
