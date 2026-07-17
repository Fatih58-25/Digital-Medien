using System.Collections.Generic;


namespace GenericBehaviorTree
{
  public class Selector : Node
  {
    public Selector() : base() { }
    public Selector(List<Node> children) : base(children) { }
    public override NodeState Evaluate()
    {
      /*
        - If no child node returns a success state, the selector will immediately return failure. and continue to the next child.
        - If any child node returns a success state, the selector will immediately return success. and start from the first child again.
        - If any child node returns a running state, the selector will immediately return running. and start from the running child again.
      */

      /*
        fail => next child
        success => stop and start from the first child again
        running => stop and start from the running child again
      */
      foreach (Node node in children)
      {
        switch (node.Evaluate())
        {
          case NodeState.FAILURE:
            continue;
          case NodeState.SUCCESS:
            state = NodeState.SUCCESS;
            return state;
          case NodeState.RUNNING:
            state = NodeState.RUNNING;
            return state;
          default:
            continue;
        }
      }
      state = NodeState.FAILURE;
      return state;
    }

  }
}