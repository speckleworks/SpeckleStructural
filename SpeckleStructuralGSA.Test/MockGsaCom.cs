using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interop.Gsa_10_0;

namespace SpeckleStructuralGSA.Test
{
  public class MockGsaCom : IComAuto
  {
    public short Open(string sPathName)
    {
      throw new NotImplementedException();
    }

    public short Analyse(object Task)
    {
      throw new NotImplementedException();
    }

    public short Save()
    {
      throw new NotImplementedException();
    }

    public short SaveAs(string sPathName)
    {
      throw new NotImplementedException();
    }

    public short Delete(string sContent)
    {
      throw new NotImplementedException();
    }

    public short Close()
    {
      throw new NotImplementedException();
    }

    public void UpdateViews()
    {
      throw new NotImplementedException();
    }

    public short NewFile()
    {
      throw new NotImplementedException();
    }

    public dynamic GwaCommand(string sCommand)
    {
      if (sCommand.Contains("GET"))
      {
        return "";
      }
      return 1;
    }

    public short PrintView(string sView)
    {
      throw new NotImplementedException();
    }

    public int HighestView(string sViewType)
    {
      throw new NotImplementedException();
    }

    public short ViewExist(string sViewType, int iRef)
    {
      throw new NotImplementedException();
    }

    public string ViewName(string sViewType, int iRef)
    {
      throw new NotImplementedException();
    }

    public int NumArg(string sLine)
    {
      throw new NotImplementedException();
    }

    public string Arg(int iIndex, string sLine)
    {
      throw new NotImplementedException();
    }

    public short SaveViewToFile(string sView, string sFileType)
    {
      throw new NotImplementedException();
    }

    public short Output_Init(int iFlags, string sAxis, string sCase, int iDataRef, int iNumEqInt1DPos)
    {
      throw new NotImplementedException();
    }

    public short Output_SetStage(int iStage)
    {
      throw new NotImplementedException();
    }

    public string Output_DataTitle(int iFlags)
    {
      throw new NotImplementedException();
    }

    public short Output_IsDataRef(int iFlags)
    {
      throw new NotImplementedException();
    }

    public string Output_UnitString()
    {
      throw new NotImplementedException();
    }

    public float Output_UnitFactor()
    {
      throw new NotImplementedException();
    }

    public short Output_DataExist(int iRef)
    {
      throw new NotImplementedException();
    }

    public int Output_NumElemPos(int iRef)
    {
      throw new NotImplementedException();
    }

    public float Output_1DElemPos(int iPos)
    {
      throw new NotImplementedException();
    }

    public dynamic Output_Extract(int iRef, int iPos)
    {
      throw new NotImplementedException();
    }

    public int HighestCase(string sCaseOp)
    {
      throw new NotImplementedException();
    }

    public short CaseExist(string sCaseOp, int iRef)
    {
      throw new NotImplementedException();
    }

    public string CaseName(string sCaseOp, int iRef)
    {
      throw new NotImplementedException();
    }

    public int CaseNumPerm(string sCaseOp, int iRef)
    {
      throw new NotImplementedException();
    }

    public string CasePermDesc(string sCaseOp, int iRef, int iPerm)
    {
      throw new NotImplementedException();
    }

    public string CasePermString(string sCaseOp, int iRef, int iPerm)
    {
      throw new NotImplementedException();
    }

    public float CasePermAnalFactor(string sCaseOp, int iRef, int iPerm, int iAnalRef)
    {
      throw new NotImplementedException();
    }

    public int Output_Extract_CurPerm()
    {
      throw new NotImplementedException();
    }

    public short CaseResultsExist(string sCaseOp, int iRef, int iPerm)
    {
      throw new NotImplementedException();
    }

    public int CaseTask(int iRef)
    {
      throw new NotImplementedException();
    }

    public short TaskStatus(int iRef)
    {
      throw new NotImplementedException();
    }

    public int ProcessID()
    {
      throw new NotImplementedException();
    }

    public int Gen_NodeAt(double dX, double dY, double dZ, double dCoincidenceTol)
    {
      //TO DO - review
      return 1;
    }

    public string Gen_SectionMatchDesc(string sDesc, int iFlags, bool bSimple)
    {
      throw new NotImplementedException();
    }

    public string VersionString()
    {
      throw new NotImplementedException();
    }

    public int SectionUsage(int iSec, int eSel = 3, int iStage = 0)
    {
      throw new NotImplementedException();
    }

    public string Gen_SectTransltnGsRevit(string nameIn, GsRevit_SectTrnsDir eDir, GsRevit_Usage usage, ref string familyName)
    {
      throw new NotImplementedException();
    }

    public int MembNumElem(int iRef)
    {
      throw new NotImplementedException();
    }

    public int MembElemNum(int iRef, int iIdx)
    {
      throw new NotImplementedException();
    }

    public int ElemMembNum(int iRef)
    {
      throw new NotImplementedException();
    }

    public short Tool_UpdateElemSections()
    {
      throw new NotImplementedException();
    }

    public short Tool_ResetMemberSections()
    {
      throw new NotImplementedException();
    }

    public int IsItemIncluded(string sType, int iIndex, string sList)
    {
      throw new NotImplementedException();
    }

    public short Gen_RegionMeshCheck(int iIndex, out int piError, out int piWarning, out string psLog)
    {
      throw new NotImplementedException();
    }

    public short Gen_RegionMeshGen(int iIndex, out int piError, out int piWarning, out string psLog)
    {
      throw new NotImplementedException();
    }

    public short Gen_RegionMeshDel(int iIndex)
    {
      throw new NotImplementedException();
    }

    public string GetSidTagValue(string sKey, int iRecord, string sTag)
    {
      throw new NotImplementedException();
    }

    public short WriteSidTagValue(string sKey, int iRecord, string sTag, string sValue)
    {
      throw new NotImplementedException();
    }

    public short NodeCoor(int iRef, out double pX, out double pY, out double pZ)
    {
      throw new NotImplementedException();
    }

    public short Renumber_Init(GsaEntity option)
    {
      throw new NotImplementedException();
    }

    public short Renumber_SetTo(GsaEntity option, int oldRef, int newRef)
    {
      throw new NotImplementedException();
    }

    public short Renumber_Go(GsaEntity option)
    {
      throw new NotImplementedException();
    }

    public short SetLocale(Locale Locale)
    {
      throw new NotImplementedException();
    }

    public short Output_Init_Arr(int iFlags, string sAxis, string sCase, ResHeader header, int iNumEqInt1DPos)
    {
      throw new NotImplementedException();
    }

    public short Output_Extract_Arr(int iRef, out GsaResults[] arrayResults, out int NumComponents)
    {
      throw new NotImplementedException();
    }

    public short EntitiesInList(string sList, ref GsaEntity listType, out int[] arrayEntities)
    {
      throw new NotImplementedException();
    }

    public short CopyView(string sViewType, int idxView, out int idxNewView)
    {
      throw new NotImplementedException();
    }

    public short RenameView(string sViewType, int idxView, string sNewName)
    {
      throw new NotImplementedException();
    }

    public short DeleteView(string sViewType, int idxView)
    {
      throw new NotImplementedException();
    }

    public short GetViewCaseList(string sViewType, int idxView, out string sList)
    {
      throw new NotImplementedException();
    }

    public short SetViewCaseList(string sViewType, int idxView, string sList)
    {
      throw new NotImplementedException();
    }

    public short GetViewDisplayList(string sViewType, int idxView, ref GsaEntity listType, out string sList)
    {
      throw new NotImplementedException();
    }

    public short SetViewDisplayList(string sViewType, int idxView, GsaEntity listType, string sList)
    {
      throw new NotImplementedException();
    }

    public short RescaleViewData(int idxView)
    {
      throw new NotImplementedException();
    }

    public short NodeConnectedEnt(GsaEntity entityType, int nodeRef, out int[] entRefs)
    {
      throw new NotImplementedException();
    }

    public void LogFeatureUsage(string featureIn)
    {
      throw new NotImplementedException();
    }

    public short Nodes(int[] nodeRefs, out GsaNode[] objNodes)
    {
      throw new NotImplementedException();
    }

    public short SetNodes(GsaNode[] objNodesIn, bool Overwrite)
    {
      throw new NotImplementedException();
    }

    public short Elements(int[] elemRefs, out GsaElement[] objElems)
    {
      throw new NotImplementedException();
    }

    public short SetElements(GsaElement[] objElemsIn, bool Overwrite)
    {
      throw new NotImplementedException();
    }

    public short Sections(int[] sectRefs, out GsaSection[] objSections)
    {
      throw new NotImplementedException();
    }

    public short SetSections(GsaSection[] objSectsIn, bool Overwrite)
    {
      throw new NotImplementedException();
    }

    public short MembCoorOnCurve(int iMembRef, out double pX, out double pY, out double pZ)
    {
      throw new NotImplementedException();
    }

    public short MappingDBPath(out string psMappingPath)
    {
      throw new NotImplementedException();
    }

    public short Output_Extract_CutAssembly(int assembRef, bool Avg2DStress, string sCase, string sAxis, out GsaResults[] arrayResults)
    {
      throw new NotImplementedException();
    }

    public int ElemIsVertical(int iElem, int eSel = 3)
    {
      throw new NotImplementedException();
    }

    public int ElemIsHorizontal(int iElem, int eSel = 3)
    {
      throw new NotImplementedException();
    }

    public short LineCoorOnCurve(int iLineRef, out double pX, out double pY, out double pZ)
    {
      throw new NotImplementedException();
    }

    public short RegionPoints(int iRegion, out string[] arrayPoints, out int[] arrayArea, out int[] arrayVoidArea)
    {
      throw new NotImplementedException();
    }

    public int IsAreaInsideArea(int iArea, int jArea)
    {
      throw new NotImplementedException();
    }

    public int ViewRefFromName(string sViewType, string Name)
    {
      throw new NotImplementedException();
    }

    public int CreateNewView(string Name)
    {
      throw new NotImplementedException();
    }

    public short SetViewContour(int idxView, int iDataRef, string sSavedViewGwa)
    {
      throw new NotImplementedException();
    }

    public short SetViewDiagram(int idxView, int iDataRef, string sSavedViewGwa)
    {
      throw new NotImplementedException();
    }

    public short SetViewLabels(int idxView, string savedViewGwa)
    {
      throw new NotImplementedException();
    }

    public short GetViewOrientation(int idxView, out GsaViewOrientation orientation)
    {
      throw new NotImplementedException();
    }

    public short SetViewOrientation(int idxView, ref GsaViewOrientation orientation)
    {
      throw new NotImplementedException();
    }

    public short DesignTaskStatus(int idxTask)
    {
      throw new NotImplementedException();
    }

    public short DESIGN(int idxTask, DesignOption option)
    {
      throw new NotImplementedException();
    }

    public short Members(int[] membRefs, out GsaMember[] objMembs)
    {
      throw new NotImplementedException();
    }

    public short SetMembers(GsaMember[] objMembsIn, bool Overwrite)
    {
      throw new NotImplementedException();
    }

    public double Tool_GetEntLength(int idxElem, GsaEntity entityType)
    {
      throw new NotImplementedException();
    }

    public short SetViewBaseSettings(int idxView, string savedViewGwa)
    {
      throw new NotImplementedException();
    }

    public short RescaleViewToFit(int idxView)
    {
      throw new NotImplementedException();
    }

    public void DisplayGsaWindow(bool bDisplay)
    {
      throw new NotImplementedException();
    }

    public short Output_BeamForces(int iEquidistPts, string sAxis, string sCase, int iElem, out double[] results, out int numPoints)
    {
      throw new NotImplementedException();
    }

    public void ReindexCasesAndTasks()
    {
      throw new NotImplementedException();
    }
  }
}
