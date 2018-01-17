using System.Collections.Generic;
using NXOpen;

namespace TechDocNS.Model
{
    public struct DrawingsSetup
    {
        public string DrawingsFormatName;
        public int DrawingsType;
        public int SheetNums;
        public int OperationNumber;
        public NxDrawingsFromat[] DrawingsFormats;
        public string AdditionalFile;
        //public List<TaggedObject> AdditionalTools;
        public IEnumerable<NxOperationGroup> NxOperationGroups;
        public string AdditionalToolName;
        public string AdditionalToolGroupName;
        // доп.описание для элкетроискровых станков
        public List<string> SparkToolGroupNotes;
    }
}