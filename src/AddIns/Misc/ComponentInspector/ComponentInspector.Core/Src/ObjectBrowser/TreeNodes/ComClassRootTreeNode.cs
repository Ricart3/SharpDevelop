// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Oakland Software Incorporated" email="general@oaklandsoftware.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using Microsoft.Win32;

using NoGoop.Obj;
using NoGoop.ObjBrowser.Panels;
using NoGoop.Win32;
using NoGoop.Util;

namespace NoGoop.ObjBrowser.TreeNodes
{

	internal class ComClassRootTreeNode : ComRootTreeNode
	{
        internal ComClassRootTreeNode() : base()
		{
            SetPresInfo(PresentationMap.COM_FOLDER_CLASS);

            _baseKey = Windows.KeyCLSID;
            _progressName = "Class";
		}

        // Used to get the basic info that is used by this type
        // of node
        protected override Object ProcessChild(RegistryKey key,
                                                  String subKeyName)
        {
            return ComClassInfo.GetClassInfo(key, subKeyName);
        }


        // Allocates the correct type of node
        protected override BrowserTreeNode AllocateChildNode(Object obj)
        {
            BrowserTreeNode node = new ComTypeTreeNode((BasicInfo)obj);
            node.IntermediateNodeTypes = null;
            if (_progress != null)
                _progress.UpdateProgress(1);
            return node;
        }


        // Determines is this node has children
        protected override bool HasChildren()
        {
            // Assume there are classes on the system
            return true;
        }



        public override String GetName()
		{
            return "Classes";
		}

        public override void GetDetailText()
		{
            base.GetDetailText();
		}


	}

}
