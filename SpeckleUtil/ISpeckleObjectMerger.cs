using System;
using System.Collections.Generic;
using SpeckleCore;

namespace SpeckleUtil
{
  public interface ISpeckleObjectMerger
  {
    void Initialise(List<Type> typesToRecognise);

    SpeckleObject Merge(SpeckleObject src, SpeckleObject dest);
  }
}
