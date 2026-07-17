using System.Collections.Generic;


namespace GenericBehaviorTree
{
  public class Node
  {
    protected NodeState state;
    public Node parent;
    protected List<Node> children = new();
    private Dictionary<string, object> _DataContext = new();

    public Node()
    {
      parent = null;
    }

    public Node(List<Node> children)
    {
      foreach (Node child in children) _Attach(child);
    }

    private void _Attach(Node child)
    {
      child.parent = this;
      children.Add(child);
    }

    public virtual NodeState Evaluate() => NodeState.FAILURE;
    public void SetData(string key, object value)
    {
      if (_DataContext.ContainsKey(key)) _DataContext[key] = value;
      else _DataContext.Add(key, value);
    }

    public object GetData(string key)
    {
      object value = null;
      if (_DataContext.TryGetValue(key, out value)) return value;

      Node node = parent;
      while (node != null)
      {
        value = node.GetData(key);
        if (value != null) return value;
        node = node.parent;
      }
      return null;
    }


    public bool ClearData(string key)
    {
      if (_DataContext.ContainsKey(key))
      {
        _DataContext.Remove(key);
        return true;
      }
      Node node = parent;
      while (node != null)
      {
        bool cleared = node.ClearData(key);
        if (cleared) return true;
        node = node.parent;
      }
      return false;
    }
    
  }

  public enum NodeState
  {
    SUCCESS,
    FAILURE,
    RUNNING,
    IDLE
  }
}