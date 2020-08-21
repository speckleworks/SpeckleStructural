using System.Collections.Generic;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Test
{
  public class TestAppUI : ISpeckleGSAAppUI
  {
    private object msgLock = new object();
    public Dictionary<string, List<string>> Messages = new Dictionary<string, List<string>>();

    public bool Message(string headingMessage, string exampleDetail)
    {
      lock (msgLock)
      {
        if (!Messages.ContainsKey(headingMessage))
        {
          Messages.Add(headingMessage, new List<string>());
        }
        Messages[headingMessage].Add(exampleDetail);
      }
      return true;
    }
  }
}
