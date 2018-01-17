using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NXOpen;
using NXOpen.CAM;
using NXOpen.Utilities;
using TechDocNS.Services;

namespace TechDocNS.Model
{
    public class NxOperationGroup
    {
        private CAMObject _taggedObject;
        private IEnumerable<TaggedObject> TaggedObjects { get; set; }
        public IEnumerable<NxOperation> NxOperations { get; private set; }
        public string MachineName { get; set; }

        //------------------------------------------
        public static Dictionary<Tag, string> toolMap = new Dictionary<Tag, string>();
        //------------------------------------------


        public string Name
        {
            get { return _taggedObject != null ? _taggedObject.Name : string.Empty; }
        }

        public string Description
        {
            get { return "Управляющая программа;  станок " + MachineName; }
        }


        public NxOperationGroup(IEnumerable<TaggedObject> taggedObjects, string machineName)
        {
            var objects = taggedObjects as IList<TaggedObject> ?? taggedObjects.ToList();
            if (!objects.OfType<NXOpen.CAM.Operation>().Any() && !objects.OfType<NCGroup>().Any())
                throw new Exception("Не выбрана операция или группа операций!");

            MachineName = machineName;
            TaggedObjects = objects;
            _taggedObject = TaggedObjects.FirstOrDefault() as CAMObject;
            if (_taggedObject == null)
                throw new Exception("Не выбрана операция или группа операций!");

            if (_taggedObject is NXOpen.CAM.Operation)
                NxOperations = GetOperations(_taggedObject as NXOpen.CAM.Operation);
            else if (_taggedObject is NCGroup)
                NxOperations = GetOperations(_taggedObject as NCGroup);
        }

        public NxOperationGroup(TaggedObject taggedObject, string additionalToolName)
        {
            MachineName = additionalToolName;
            _taggedObject = taggedObject as CAMObject;
            // todo подумать на выбросом исключения. может просто возврат?
            if (_taggedObject == null)
                throw new Exception("Не выбрана операция или группа операций!");

            if (_taggedObject is NXOpen.CAM.Operation)
                NxOperations = GetOperations(_taggedObject as NXOpen.CAM.Operation);
            else if (_taggedObject is NCGroup)
                NxOperations = GetOperations(_taggedObject as NCGroup);
        }

        private IEnumerable<NxOperation> GetOperations(NCGroup ncGroup)
        {
            if (ncGroup == null) return null;

            var members = GetMembers(ncGroup.GetMembers());
            var operations = members.OfType<NXOpen.CAM.Operation>();

            if (NxSession.Ufs == null)
                throw new Exception("Не удалось получить сессию пользовательских функций NX.");

            Tag cutterTag;
            var enumerable = operations.Where(op =>
            {
                NxSession.Ufs.Oper.AskCutterGroup(op.Tag, out cutterTag);
                return cutterTag != Tag.Null && NXObjectManager.Get(cutterTag) is Tool;
            });

            return enumerable
                .Select(op =>
                    new NxOperation(op, this));
        }

        private IEnumerable<NxOperation> GetOperations(NXOpen.CAM.Operation op)
        {
            Tag cutterTag;
            if (op == null) yield break;

            if (NxSession.Ufs == null)
                throw new Exception("Не удалось получить сессию пользовательских функций NX.");

            NxSession.Ufs.Oper.AskCutterGroup(op.Tag, out cutterTag);
            if (cutterTag == Tag.Null || !(NXObjectManager.Get(cutterTag) is Tool)) yield break;

            yield return
                new NxOperation(op, this);
        }

        //        private IEnumerable<NxOperation> GetOperations()
        //        {
        //            var camObjects = TaggedObjects.OfType<CAMObject>();
        //            var operations = GetMembers(camObjects).OfType<Operation>();
        //
        //            if (NxDrawingCreator.ufs == null)
        //                throw new Exception("Не удалось получить сессию пользовательских функций NX.");
        //
        //            Tag cutterTag;
        //            var enumerable = operations.Where(op =>
        //            {
        //                NxDrawingCreator.ufs.Oper.AskCutterGroup(op.Tag, out cutterTag);
        //                return cutterTag != Tag.Null && NXObjectManager.Get(cutterTag) is Tool;
        //            });
        //
        //            return enumerable
        //                .Select(op => 
        //                    new NxOperation(op.Tag, MachineName)).ToList();
        //        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private static IEnumerable<CAMObject> GetMembers(IEnumerable<CAMObject> list)
        {
            return list.Concat(list.OfType<NCGroup>()
                .SelectMany(gr => GetMembers(gr.GetMembers())));
        }
    }
}