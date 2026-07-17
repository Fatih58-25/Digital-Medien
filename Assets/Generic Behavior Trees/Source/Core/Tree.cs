using UnityEngine;


namespace GenericBehaviorTree
{
  //
  // Summary:
  //     Tree class is the base class for all behaviour trees. It is responsible for updating
  public abstract class Tree : MonoBehaviour
  {
    private Node _root = null;

    protected void Start()
    {
      _root = SetupTree();
    }

    private void Update()
    {
      if (_root != null) _root.Evaluate();
    }

    protected abstract Node SetupTree();

  }
}