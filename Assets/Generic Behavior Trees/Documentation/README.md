Sure! Here is a `README.md` file for your Unity asset, "Generic Behaviour Tree":

```markdown
# Generic Behaviour Tree

## Introduction

**Generic Behaviour Tree** is a Unity asset that provides a flexible and reusable behaviour tree implementation. Behaviour trees are used to define the decision-making process of AI characters in games. This asset allows you to create complex AI behaviours by combining simple tasks and decisions in a tree structure.

## Installation

1. Download the `GenericBehaviorTree` package from the Unity Asset Store or from the provided link.
2. Import the package into your Unity project:
   - Open Unity and go to `Assets > Import Package > Custom Package`.
   - Select the downloaded package and click `Import`.

## Usage

To use the Generic Behaviour Tree in your Unity project, follow these steps:

1. Create a new script that inherits from `Tree` and override the `SetupTree` method to define the behaviour tree structure.
2. Add the custom script to a GameObject in your scene.
3. Implement the necessary Node classes for your AI behaviour.

## Node States

In the Generic Behaviour Tree, nodes can be in one of the following states:

- **SUCCESS**: Indicates that the node has completed its task successfully.
- **FAILURE**: Indicates that the node has failed to complete its task.
- **RUNNING**: Indicates that the node is currently executing its task and has not yet finished.
- **IDLE**: Indicates that the node is not currently active.

### NodeState Enumeration

The `NodeState` enumeration defines the possible states a node can be in:

```csharp
public enum NodeState
{
    SUCCESS,
    FAILURE,
    RUNNING,
    IDLE
}
```

## Classes and Methods

### Tree Class

The `Tree` class is the base class for all behaviour trees. It is responsible for updating the tree and managing the root node.

- **Methods:**
  - `Start()`: Initializes the root node by calling the `SetupTree` method.
  - `Update()`: Evaluates the root node each frame.
  - `SetupTree()`: Abstract method to be overridden by subclasses to define the tree structure.

### Node Class

The `Node` class represents a single node in the behaviour tree. Nodes can have children and manage their own state.

- **Properties:**
  - `NodeState state`: The current state of the node (SUCCESS, FAILURE, RUNNING, IDLE).
  - `Node parent`: The parent node.
  - `List<Node> children`: The list of child nodes.
- **Methods:**
  - `Evaluate()`: Virtual method to evaluate the node's behaviour.
  - `SetData(string key, object value)`: Stores data in the node's context.
  - `GetData(string key)`: Retrieves data from the node's context.
  - `ClearData(string key)`: Clears data from the node's context.

### Sequence Class

The `Sequence` class is a composite node that evaluates its children in sequence. If any child fails, the sequence fails. If all children succeed, the sequence succeeds.

- **Methods:**
  - `Evaluate()`: Evaluates each child in sequence.

### Selector Class

The `Selector` class is a composite node that evaluates its children until one succeeds. If any child succeeds, the selector succeeds. If all children fail, the selector fails.

- **Methods:**
  - `Evaluate()`: Evaluates each child in order.

## Examples

### Example: GreenBehaviour

```csharp
using System.Collections.Generic;
using GenericBehaviorTree;

namespace GenericBehaviorTreeTest
{
    public class GreenBehaviour : Tree
    {
        protected override Node SetupTree()
        {
            Node root = new Selector(new List<Node>
            {
                new Sequence(new List<Node>() {
                    new LookingForEnemy(transform),
                    new WalkToEnemy(transform),
                    new AttackEnemy(transform)
                }),
                new PatrolBehaviour(transform)
            });
            return root;
        }
    }
}
```

This example demonstrates how to create a custom behaviour tree for an AI character. The `AIBehaviour` class inherits from `Tree` and defines a selector node with a sequence of tasks to look for an enemy, walk to the enemy, and attack the enemy. If none of these tasks succeed, the AI will patrol the area.

## FAQs

**Q: How do I add custom nodes?**

A: To add custom nodes, create a new class that inherits from `Node` and override the `Evaluate` method to define the node's behaviour.

**Q: Can I use this asset with existing Unity AI components?**

A: Yes, you can integrate this asset with Unity's built-in AI components such as `NavMeshAgent`.

## Support

For support, please contact [your email/contact information]. You can also refer to the [official documentation link] for more detailed information.

```

Feel free to customize this README file further to include additional information specific to your asset.