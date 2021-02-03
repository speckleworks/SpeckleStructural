using System;
using System.Collections.Generic;
using SpeckleGSAInterfaces;

namespace SpeckleStructuralGSA.Test
{
  public class MockGSAMessenger : IGSAMessenger
  {
    public List<Tuple<MessageIntent, MessageLevel, string[]>> Messages = new List<Tuple<MessageIntent, MessageLevel, string[]>>();

    public bool CacheMessage(MessageIntent intent, MessageLevel level, params string[] messagePortions)
    {
      return Message(intent, level, messagePortions);
    }

    public bool CacheMessage(MessageIntent intent, MessageLevel level, Exception ex, params string[] messagePortions)
    {
      return Message(intent, level, messagePortions);
    }

    public bool Message(MessageIntent intent, MessageLevel level, params string[] messagePortions)
    {
      Messages.Add(new Tuple<MessageIntent, MessageLevel, string[]>(intent, level, messagePortions));
      return true;
    }
  }
}
