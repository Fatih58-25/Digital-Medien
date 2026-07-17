using System.Collections.Generic;
using GenericBehaviorTree;
using UnityEngine;

namespace GenericBehaviorTreeTest
{
    public class AIBehaviour : GenericBehaviorTree.Tree
    {

        protected override Node SetupTree()
        {
            Node root = new Selector(new List<Node>
            {
                    // The sequence will evaluate the nodes in order.
                    // If a node returns failure, the sequence will return failure.
                    // If a node returns running, the sequence will return running.
                    // If a node returns success, the sequence will move to the next node.
                new Sequence(new List<Node>() {
                    new LookingForEnemy(transform),
                    new WalkToEnemy(transform),
                    new AttackEnemy(transform)
                }),
                // The patrol behaviour will be evaluated if the sequence fails or success.
                new PatrolBehaviour(transform)
            });
            return root;
        }
    }

}