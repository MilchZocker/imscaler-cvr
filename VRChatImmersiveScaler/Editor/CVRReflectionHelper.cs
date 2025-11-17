using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace VRChatImmersiveScaler.Editor
{
    /// <summary>
    /// Helper class to access CVR CCK types via reflection (similar to how Chillaxins does it)
    /// </summary>
    public static class CVRReflectionHelper
    {
        private const string CVRAvatar_ClassFullName = "ABI.CCK.Components.CVRAvatar";
        
        private static Type _cvrAvatarType;
        private static FieldInfo _viewPositionField;
        private static FieldInfo _voicePositionField;
        
        static CVRReflectionHelper()
        {
            // Find CVRAvatar type via reflection
            _cvrAvatarType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .FirstOrDefault(t => t.FullName == CVRAvatar_ClassFullName);
            
            if (_cvrAvatarType != null)
            {
                _viewPositionField = _cvrAvatarType.GetField("viewPosition", BindingFlags.Public | BindingFlags.Instance);
                _voicePositionField = _cvrAvatarType.GetField("voicePosition", BindingFlags.Public | BindingFlags.Instance);
            }
        }
        
        public static bool IsCVRCCKAvailable()
        {
            return _cvrAvatarType != null;
        }
        
        public static Component GetCVRAvatar(GameObject avatar)
        {
            if (_cvrAvatarType == null) return null;
            return avatar.GetComponent(_cvrAvatarType);
        }
        
        public static Vector3 GetViewPosition(Component cvrAvatar)
        {
            if (_viewPositionField == null || cvrAvatar == null) return Vector3.zero;
            return (Vector3)_viewPositionField.GetValue(cvrAvatar);
        }
        
        public static void SetViewPosition(Component cvrAvatar, Vector3 position)
        {
            if (_viewPositionField == null || cvrAvatar == null) return;
            _viewPositionField.SetValue(cvrAvatar, position);
        }
        
        public static Vector3 GetVoicePosition(Component cvrAvatar)
        {
            if (_voicePositionField == null || cvrAvatar == null) return Vector3.zero;
            return (Vector3)_voicePositionField.GetValue(cvrAvatar);
        }
        
        public static void SetVoicePosition(Component cvrAvatar, Vector3 position)
        {
            if (_voicePositionField == null || cvrAvatar == null) return;
            _voicePositionField.SetValue(cvrAvatar, position);
        }
    }
}
