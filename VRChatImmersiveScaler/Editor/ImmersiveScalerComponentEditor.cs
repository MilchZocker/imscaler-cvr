using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatImmersiveScaler.Editor
{
    // Component parameter provider wrapper
    internal class ComponentParameterProvider : ImmersiveScalerUIShared.IParameterProvider
    {
        private ImmersiveScalerComponent component;

        public ComponentParameterProvider(ImmersiveScalerComponent comp)
        {
            component = comp;
        }

        // Basic Settings
        public float targetHeight
        {
            get => component.targetHeight;
            set => component.targetHeight = value;
        }

        public float upperBodyPercentage
        {
            get => component.upperBodyPercentage;
            set => component.upperBodyPercentage = value;
        }

        public float customScaleRatio
        {
            get => component.customScaleRatio;
            set => component.customScaleRatio = value;
        }

        // Body Proportions
        public float armThickness
        {
            get => component.armThickness;
            set => component.armThickness = value;
        }

        public float legThickness
        {
            get => component.legThickness;
            set => component.legThickness = value;
        }

        public float thighPercentage
        {
            get => component.thighPercentage;
            set => component.thighPercentage = value;
        }

        // Scaling Options
        public bool scaleHand
        {
            get => component.scaleHand;
            set => component.scaleHand = value;
        }

        public bool scaleFoot
        {
            get => component.scaleFoot;
            set => component.scaleFoot = value;
        }

        public bool scaleEyes
        {
            get => component.scaleEyes;
            set => component.scaleEyes = value;
        }

        public bool centerModel
        {
            get => component.centerModel;
            set => component.centerModel = value;
        }

        // Advanced Options
        public float extraLegLength
        {
            get => component.extraLegLength;
            set => component.extraLegLength = value;
        }

        public bool scaleRelative
        {
            get => component.scaleRelative;
            set => component.scaleRelative = value;
        }

        public float armToLegs
        {
            get => component.armToLegs;
            set => component.armToLegs = value;
        }

        public bool keepHeadSize
        {
            get => component.keepHeadSize;
            set => component.keepHeadSize = value;
        }

        // Debug Options
        public bool skipMainRescale
        {
            get => component.skipMainRescale;
            set => component.skipMainRescale = value;
        }

        public bool skipMoveToFloor
        {
            get => component.skipMoveToFloor;
            set => component.skipMoveToFloor = value;
        }

        public bool skipHeightScaling
        {
            get => component.skipHeightScaling;
            set => component.skipHeightScaling = value;
        }

        public bool useBoneBasedFloorCalculation
        {
            get => component.useBoneBasedFloorCalculation;
            set => component.useBoneBasedFloorCalculation = value;
        }

        // Additional Tools
        public bool applyFingerSpreading
        {
            get => component.applyFingerSpreading;
            set => component.applyFingerSpreading = value;
        }

        public float fingerSpreadFactor
        {
            get => component.fingerSpreadFactor;
            set => component.fingerSpreadFactor = value;
        }

        public bool spareThumb
        {
            get => component.spareThumb;
            set => component.spareThumb = value;
        }

        public bool applyShrinkHipBone
        {
            get => component.applyShrinkHipBone;
            set => component.applyShrinkHipBone = value;
        }

        // Measurement methods
        public HeightMethodType targetHeightMethod
        {
            get => component.targetHeightMethod;
            set => component.targetHeightMethod = value;
        }

        public ArmMethodType armToHeightRatioMethod
        {
            get => component.armToHeightRatioMethod;
            set => component.armToHeightRatioMethod = value;
        }

        public HeightMethodType armToHeightHeightMethod
        {
            get => component.armToHeightHeightMethod;
            set => component.armToHeightHeightMethod = value;
        }

        public bool upperBodyUseNeck
        {
            get => component.upperBodyUseNeck;
            set => component.upperBodyUseNeck = value;
        }

        public bool upperBodyTorsoUseNeck
        {
            get => component.upperBodyTorsoUseNeck;
            set => component.upperBodyTorsoUseNeck = value;
        }

        public bool upperBodyUseLegacy
        {
            get => component.upperBodyUseLegacy;
            set => component.upperBodyUseLegacy = value;
        }

        // Debug visualization
        public string debugMeasurement
        {
            get => component.debugMeasurement;
            set => component.debugMeasurement = value;
        }

        public void SetDirty()
        {
            EditorUtility.SetDirty(component);
        }
    }

    [CustomEditor(typeof(ImmersiveScalerComponent))]
    public class ImmersiveScalerComponentEditor : UnityEditor.Editor
    {
        private ImmersiveScalerCore scalerCore;
        private ComponentParameterProvider paramProvider;
        private bool showAdvanced = false;
        private bool showDebug = false;
        private bool showCurrentStats = true;
        private bool showAdditionalTools = false;
        private bool showDebugMeasurements = false;
        private bool showDebugRatios = false;

        // Preview state tracking
        private bool isPreviewActive = false;
        private Dictionary<Transform, TransformState> originalTransformStates = new Dictionary<Transform, TransformState>();
        private Component previewAvatar = null;
        private Vector3 storedOriginalViewPosition;

        // Helper class to store transform state
        private class TransformState
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;

            public TransformState(Transform t)
            {
                localPosition = t.localPosition;
                localRotation = t.localRotation;
                localScale = t.localScale;
            }

            public void RestoreTo(Transform t)
            {
                t.localPosition = localPosition;
                t.localRotation = localRotation;
                t.localScale = localScale;
            }
        }

        private void OnEnable()
        {
            var component = (ImmersiveScalerComponent)target;
            paramProvider = new ComponentParameterProvider(component);

            var avatar = CVRReflectionHelper.GetCVRAvatar(component.gameObject);
            if (avatar == null)
            {
                // Try parent
                Transform parent = component.transform.parent;
                while (parent != null && avatar == null)
                {
                    avatar = CVRReflectionHelper.GetCVRAvatar(parent.gameObject);
                    parent = parent.parent;
                }
            }

            if (avatar != null)
            {
                scalerCore = new ImmersiveScalerCore(avatar.gameObject);

                // Auto-populate values if they're at defaults
                if (Mathf.Approximately(component.targetHeight, 1.61f) &&
                    Mathf.Approximately(component.upperBodyPercentage, 44f) &&
                    Mathf.Approximately(component.customScaleRatio, 0.4537f))
                {
                    AutoPopulateValues(component);
                }
            }

            // Subscribe to scene GUI
            SceneView.duringSceneGui += OnSceneGUI;

            // Subscribe to selection changes to auto-cancel preview
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            // Unsubscribe from scene GUI
            SceneView.duringSceneGui -= OnSceneGUI;

            // Unsubscribe from selection changes
            Selection.selectionChanged -= OnSelectionChanged;

            // Cancel preview if active
            if (isPreviewActive)
            {
                var component = (ImmersiveScalerComponent)target;
                if (component != null)
                {
                    var avatar = GetAvatarComponent(component);
                    if (avatar != null)
                    {
                        ResetPreview(component, avatar);
                        return;
                    }
                }

                // If component is null, use stored references
                if (previewAvatar != null)
                {
                    ResetPreviewWithStoredReferences();
                }
            }
        }

        private Component GetAvatarComponent(ImmersiveScalerComponent component)
        {
            var avatar = CVRReflectionHelper.GetCVRAvatar(component.gameObject);
            if (avatar == null)
            {
                Transform parent = component.transform.parent;
                while (parent != null && avatar == null)
                {
                    avatar = CVRReflectionHelper.GetCVRAvatar(parent.gameObject);
                    parent = parent.parent;
                }
            }
            return avatar;
        }

        private void OnDestroy()
        {
            if (isPreviewActive && previewAvatar != null)
            {
                ResetPreviewWithStoredReferences();
            }
        }

        private void OnSelectionChanged()
        {
            if (isPreviewActive && target != null)
            {
                var component = (ImmersiveScalerComponent)target;
                bool isStillSelected = false;

                foreach (var obj in Selection.objects)
                {
                    if (obj == component || obj == component.gameObject)
                    {
                        isStillSelected = true;
                        break;
                    }
                }

                if (!isStillSelected)
                {
                    var avatar = GetAvatarComponent(component);
                    if (avatar != null)
                    {
                        ResetPreview(component, avatar);
                    }
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            var component = (ImmersiveScalerComponent)target;
            if (string.IsNullOrEmpty(component.debugMeasurement)) return;
            if (scalerCore == null) return;

            var avatar = GetAvatarComponent(component);
            if (avatar == null) return;

            ImmersiveScalerUIShared.DrawMeasurementWithHandles(component.debugMeasurement, scalerCore, paramProvider, avatar);
        }

        public override void OnInspectorGUI()
        {
            var component = (ImmersiveScalerComponent)target;
            var avatar = GetAvatarComponent(component);

            if (avatar == null)
            {
                if (CVRReflectionHelper.IsCVRCCKAvailable())
                {
                    EditorGUILayout.HelpBox("This component must be on a ChilloutVR avatar with a CVRAvatar component!", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("ChilloutVR CCK is not installed!", MessageType.Error);
                }
                return;
            }

            // Update scaler core if needed
            if (scalerCore == null || scalerCore.avatarRoot != avatar.gameObject)
            {
                scalerCore = new ImmersiveScalerCore(avatar.gameObject);
            }

            serializedObject.Update();

            // Get viewPosition safely
            Vector3 origViewPos = component.hasStoredOriginalViewPosition ? 
                component.originalViewPosition : CVRReflectionHelper.GetViewPosition(avatar);

            // Current Stats Section
            ImmersiveScalerUIShared.DrawCurrentStatsSection(paramProvider, scalerCore, avatar, ref showCurrentStats, isPreviewActive, origViewPos);

            // Measurement Config Section
            ImmersiveScalerUIShared.DrawMeasurementConfigSection(paramProvider, scalerCore, ref showDebugMeasurements, ref showDebugRatios);

            EditorGUILayout.Space();

            // Basic Settings
            ImmersiveScalerUIShared.DrawBasicSettingsSection(paramProvider, scalerCore, serializedObject);

            EditorGUILayout.Space();

            // Body Proportions
            ImmersiveScalerUIShared.DrawBodyProportionsSection(paramProvider, scalerCore, serializedObject);

            EditorGUILayout.Space();

            // Scaling Options
            ImmersiveScalerUIShared.DrawScalingOptionsSection(paramProvider, serializedObject);

            EditorGUILayout.Space();

            // Advanced Options
            ImmersiveScalerUIShared.DrawAdvancedOptionsSection(paramProvider, ref showAdvanced, serializedObject);

            EditorGUILayout.Space();

            // Additional Tools
            ImmersiveScalerUIShared.DrawAdditionalToolsSection(paramProvider, ref showAdditionalTools, serializedObject);

            EditorGUILayout.Space();

            // Action Buttons
            if (!isPreviewActive)
            {
                GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                if (GUILayout.Button("Preview Scaling", GUILayout.Height(30)))
                {
                    StartPreview(component, avatar);
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("Preview Mode Active - Showing how avatar will look after build", MessageType.Info);
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("Cancel Preview", GUILayout.Height(30)))
                {
                    ResetPreview(component, avatar);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.HelpBox("Scaling will be applied automatically when building/uploading the avatar via NDMF.", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }

        private void StartPreview(ImmersiveScalerComponent component, Component avatar)
        {
            previewAvatar = avatar;
            storedOriginalViewPosition = CVRReflectionHelper.GetViewPosition(avatar);
            PreviewScaling(component, avatar);
        }

        private void PreviewScaling(ImmersiveScalerComponent component, Component avatar)
        {
            // Store original state
            originalTransformStates.Clear();
            Transform[] allTransforms = avatar.GetComponentsInChildren<Transform>();
            foreach (var t in allTransforms)
            {
                originalTransformStates[t] = new TransformState(t);
            }

            Undo.RecordObject(avatar.transform, "Preview Immersive Scaling");
            foreach (var t in allTransforms)
            {
                Undo.RecordObject(t, "Preview Immersive Scaling");
            }
            Undo.RecordObject(avatar, "Preview Immersive Scaling");

            // Store original viewPosition
            if (!component.hasStoredOriginalViewPosition)
            {
                component.originalViewPosition = CVRReflectionHelper.GetViewPosition(avatar);
                component.hasStoredOriginalViewPosition = true;
            }

            // Apply scaling
            var scalerCore = new ImmersiveScalerCore(avatar.gameObject);
            Vector3 originalAvatarScale = avatar.transform.localScale;

            var parameters = new ScalingParameters
            {
                targetHeight = component.targetHeight,
                upperBodyPercentage = component.upperBodyPercentage,
                customScaleRatio = component.customScaleRatio,
                armThickness = component.armThickness,
                legThickness = component.legThickness,
                thighPercentage = component.thighPercentage,
                scaleHand = component.scaleHand,
                scaleFoot = component.scaleFoot,
                scaleEyes = component.scaleEyes,
                centerModel = component.centerModel,
                extraLegLength = component.extraLegLength,
                scaleRelative = component.scaleRelative,
                armToLegs = component.armToLegs,
                keepHeadSize = component.keepHeadSize,
                skipAdjust = component.skipMainRescale,
                skipFloor = component.skipMoveToFloor,
                skipScale = component.skipHeightScaling,
                useBoneBasedFloorCalculation = component.useBoneBasedFloorCalculation,
                targetHeightMethod = component.targetHeightMethod,
                armToHeightRatioMethod = component.armToHeightRatioMethod,
                armToHeightHeightMethod = component.armToHeightHeightMethod,
                upperBodyUseNeck = component.upperBodyUseNeck,
                upperBodyTorsoUseNeck = component.upperBodyTorsoUseNeck,
                upperBodyUseLegacy = component.upperBodyUseLegacy
            };

            scalerCore.ScaleAvatar(parameters);

            // Update viewPosition
            Vector3 newAvatarScale = avatar.transform.localScale;
            float scaleRatio = newAvatarScale.y / originalAvatarScale.y;
            CVRReflectionHelper.SetViewPosition(avatar, component.originalViewPosition * scaleRatio);

            // Apply additional tools if enabled
            if (component.applyFingerSpreading)
            {
                ImmersiveScalerFingerUtility.SpreadFingers(avatar.gameObject,
                    component.fingerSpreadFactor, component.spareThumb);
            }

            if (component.applyShrinkHipBone)
            {
                ApplyHipBoneFix(avatar);
            }

            EditorUtility.SetDirty(avatar);
            EditorUtility.SetDirty(avatar.gameObject);

            isPreviewActive = true;
        }

        private void ResetPreview(ImmersiveScalerComponent component, Component avatar)
        {
            if (!isPreviewActive) return;

            if (component.hasStoredOriginalViewPosition)
            {
                CVRReflectionHelper.SetViewPosition(avatar, component.originalViewPosition);
                EditorUtility.SetDirty(avatar);
            }

            foreach (var kvp in originalTransformStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Value.RestoreTo(kvp.Key);
                    EditorUtility.SetDirty(kvp.Key);
                }
            }

            originalTransformStates.Clear();
            isPreviewActive = false;
            previewAvatar = null;

            EditorUtility.SetDirty(avatar);
            EditorUtility.SetDirty(avatar.gameObject);
        }

        private void ResetPreviewWithStoredReferences()
        {
            if (!isPreviewActive || previewAvatar == null) return;

            CVRReflectionHelper.SetViewPosition(previewAvatar, storedOriginalViewPosition);
            EditorUtility.SetDirty(previewAvatar);

            foreach (var kvp in originalTransformStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Value.RestoreTo(kvp.Key);
                    EditorUtility.SetDirty(kvp.Key);
                }
            }

            originalTransformStates.Clear();
            isPreviewActive = false;

            EditorUtility.SetDirty(previewAvatar);
            EditorUtility.SetDirty(previewAvatar.gameObject);
            previewAvatar = null;
        }

        private void AutoPopulateValues(ImmersiveScalerComponent component)
        {
            if (scalerCore == null) return;

            float height = component.scaleEyes ?
                scalerCore.GetEyeHeight() - scalerCore.GetLowestPoint() :
                scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint();

            component.targetHeight = height;

            float upperBodyRatio;
            if (component.upperBodyUseLegacy)
            {
                upperBodyRatio = scalerCore.GetUpperBodyPortion();
            }
            else
            {
                upperBodyRatio = scalerCore.GetUpperBodyRatio(component.upperBodyUseNeck, component.upperBodyTorsoUseNeck);
            }
            component.upperBodyPercentage = upperBodyRatio * 100f;

            float armValue = scalerCore.GetArmByMethod(component.armToHeightRatioMethod);
            float heightValue = scalerCore.GetHeightByMethod(component.armToHeightHeightMethod);
            component.customScaleRatio = heightValue > 0 ? armValue / (heightValue - 0.005f) : 0.4537f;

            component.armThickness = scalerCore.GetCurrentArmThickness() * 100f;
            component.legThickness = scalerCore.GetCurrentLegThickness() * 100f;
            component.thighPercentage = scalerCore.GetThighPercentage() * 100f;

            EditorUtility.SetDirty(component);
        }

        private void ApplyHipBoneFix(Component avatar)
        {
            if (avatar == null) return;

            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return;

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);

            if (hips == null || spine == null || leftLeg == null || rightLeg == null)
            {
                Debug.LogError("Cannot find required bones for hip shrinking");
                return;
            }

            float legStartY = (leftLeg.position.y + rightLeg.position.y) / 2f;
            float spineStartY = spine.position.y;

            Vector3 newPosition = hips.position;
            newPosition.y = legStartY + (spineStartY - legStartY) * 0.9f;
            newPosition.x = spine.position.x;
            newPosition.z = spine.position.z;
            hips.position = newPosition;
        }
    }
}
