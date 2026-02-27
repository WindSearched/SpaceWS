#if UNITY_5_6_OR_NEWER

namespace XLua
{
    using UnityEngine;
    using UnityEditor;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    [InitializeOnLoad]
    public class Report
    {
        private const string PREFS_KEY = "XLuaReport";
        private const string DIALOG_MSG_FORMAT = @"���Ƿǳ�ע��������˽Ȩ����Ҫ�ռ����±�Ҫ��Ϣ���ṩ���õķ���

XLua�汾��{0}
����汾��{1}
�豸��ʶ��{2}

We attach great importance to your privacy and need to collect the following necessary information to provide better services:

XLua Version: {0}
Unity Version: {1}
Device Identifier: {2}";

    }
}

#endif
