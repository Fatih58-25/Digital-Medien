using System.Collections.Generic;


namespace GenericBehaviorTree
{
  public class Sequence : Node
  {
    public Sequence() : base() { }
    public Sequence(List<Node> children) : base(children) { }
    public override NodeState Evaluate()
    {
      bool anyChildIsRunning = false;

      /*
        fail => stop and return failure
        success => next child
        running => stop and return running
      */

      foreach (Node node in children)
      {
        switch (node.Evaluate())
        {
          case NodeState.FAILURE:
            state = NodeState.FAILURE;
            return state;
          case NodeState.SUCCESS:
            continue;
          case NodeState.RUNNING:
            anyChildIsRunning = true;
            continue;
          default:
            state = NodeState.SUCCESS;
            return state;
        }
      }
      state = anyChildIsRunning ? NodeState.RUNNING : NodeState.SUCCESS;
      return state;
    }

  }
}