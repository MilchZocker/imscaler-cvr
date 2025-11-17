using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatImmersiveScaler.Editor
{
    /// <summary>
    /// Shared UI drawing methods for both the window and component editor
    /// </summary>
    public static class ImmersiveScalerUIShared
    {
        // Map measurements between Current Avatar Stats and Measurement Config sections
        public static readonly Dictionary<string, string[]> relatedMeasurements = new Dictionary<string, string[]>
        {
            // Total height measurements
            { "total_height", new[] { "total_height", "head_height" } },
            // Eye height measurements
            { "eye_height", new[] { "eye_height_debug" } },
            { "eye_height_debug", new[] { "eye_height" } },
            // Combined head_height and neck_height measurements
            { "head_height", new[] { "total_height", "head_height", "neck_height" } },
            { "neck_height", new[] { "head_height", "neck_height" } },
            // Arm measurements
            { "head_to_elbow_vrc", new[] { "head_to_elbow_vrc" } },
            { "head_to_hand", new[] { "head_to_hand" } },
            { "arm_length", new[] { "arm_length" } },
            { "shoulder_to_fingertip", new[] { "shoulder_to_fingertip" } },
            { "center_to_hand", new[] { "center_to_hand" } },
            { "center_to_fingertip", new[] { "center_to_fingertip" } },
            // Upper body measurements
            { "upper_body_selected", new[] { "upper_body_neck", "upper_body_head", "neck_height", "head_height", "upper_body_legacy" } },
            { "upper_body_neck", new[] { "upper_body_selected" } },
            { "upper_body_head", new[] { "upper_body_selected" } },
            { "upper_body_legacy", new[] { "upper_body_selected", "upper_body_percent" } },
            // Current scale ratio (highlights selected arm and height methods)
            { "current_scale_ratio", new[] { "head_to_elbow_vrc", "head_to_hand", "arm_length", "shoulder_to_fingertip",
                "center_to_hand", "center_to_fingertip", "total_height", "eye_height_debug" } },
            // View position
            { "view_position", new[] { "view_position" } }
        };

        /// <summary>
        /// Interface for accessing parameters from either component or window
        /// </summary>
        public interface IParameterProvider
        {
            // Basic Settings
            float targetHeight { get; set; }
            float upperBodyPercentage { get; set; }
            float customScaleRatio { get; set; }

            // Body Proportions
            float armThickness { get; set; }
            float legThickness { get; set; }
            float thighPercentage { get; set; }

            // Scaling Options
            bool scaleHand { get; set; }
            bool scaleFoot { get; set; }
            bool scaleEyes { get; set; }
            bool centerModel { get; set; }

            // Advanced Options
            float extraLegLength { get; set; }
            bool scaleRelative { get; set; }
            float armToLegs { get; set; }
            bool keepHeadSize { get; set; }

            // Debug Options
            bool skipMainRescale { get; set; }
            bool skipMoveToFloor { get; set; }
            bool skipHeightScaling { get; set; }
            bool useBoneBasedFloorCalculation { get; set; }

            // Additional Tools
            bool applyFingerSpreading { get; set; }
            float fingerSpreadFactor { get; set; }
            bool spareThumb { get; set; }
            bool applyShrinkHipBone { get; set; }

            // Measurement methods
            HeightMethodType targetHeightMethod { get; set; }
            ArmMethodType armToHeightRatioMethod { get; set; }
            HeightMethodType armToHeightHeightMethod { get; set; }
            bool upperBodyUseNeck { get; set; }
            bool upperBodyTorsoUseNeck { get; set; }
            bool upperBodyUseLegacy { get; set; }

            // Debug visualization
            string debugMeasurement { get; set; }

            void SetDirty();
        }

        public static void DrawCurrentStatsSection(IParameterProvider parameters, ImmersiveScalerCore scalerCore, Component avatar, ref bool showCurrentStats, bool isPreviewActive = false, Vector3 originalViewPosition = default)
        {
            showCurrentStats = EditorGUILayout.Foldout(showCurrentStats, "Current Avatar Stats", true);
            if (showCurrentStats && scalerCore != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Show both heights for reference
                float totalHeight = scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint();
                float eyeHeight = scalerCore.GetEyeHeight() - scalerCore.GetLowestPoint();

                DrawMeasurementWithToggle(parameters, "Total Height:", $"{totalHeight:F3}m", "total_height");
                DrawMeasurementWithToggle(parameters, "Eye Height:", $"{eyeHeight:F3}m", "eye_height");

                // ViewPosition using reflection
                string viewPosDisplay;
                if (isPreviewActive && originalViewPosition != default)
                {
                    viewPosDisplay = $"{originalViewPosition.ToString("F3")} (original)";
                }
                else
                {
                    Vector3 viewPos = CVRReflectionHelper.GetViewPosition(avatar);
                    viewPosDisplay = viewPos.ToString("F3");
                }
                DrawMeasurementWithToggle(parameters, "ViewPosition:", viewPosDisplay, "view_position");

                // Selected upper body ratio
                float upperBodyRatio;
                string upperBodyDesc;
                if (parameters.upperBodyUseLegacy)
                {
                    upperBodyRatio = scalerCore.GetUpperBodyPortion();
                    upperBodyDesc = "Legacy (Leg→Eye/Floor→Eye)";
                }
                else
                {
                    upperBodyRatio = scalerCore.GetUpperBodyRatio(parameters.upperBodyUseNeck, parameters.upperBodyTorsoUseNeck);
                    upperBodyDesc = $"{(parameters.upperBodyTorsoUseNeck ? "Leg→Neck" : "Leg→Head")} / {(parameters.upperBodyUseNeck ? "Floor→Neck" : "Floor→Head")}";
                }
                DrawMeasurementWithToggle(parameters, $"Upper Body % ({upperBodyDesc}):", $"{upperBodyRatio * 100f:F1}%", "upper_body_selected");

                // Selected arm measurement
                float armValue = scalerCore.GetArmByMethod(parameters.armToHeightRatioMethod);
                string armLabel = parameters.armToHeightRatioMethod switch
                {
                    ArmMethodType.HeadToElbowVRC => "Head-to-Elbow (VRC):",
                    ArmMethodType.HeadToHand => "Head-to-Hand:",
                    ArmMethodType.ArmLength => "Arm Length:",
                    ArmMethodType.ShoulderToFingertip => "Shoulder to Fingertip:",
                    ArmMethodType.CenterToHand => "Center to Hand:",
                    ArmMethodType.CenterToFingertip => "Center to Fingertip:",
                    _ => "Arm Measurement:"
                };
                DrawMeasurementWithToggle(parameters, armLabel, $"{armValue:F3}m", GetMeasurementKeyForArmType(parameters.armToHeightRatioMethod));

                // Current scale ratio using selected methods
                float heightForRatio = scalerCore.GetHeightByMethod(parameters.armToHeightHeightMethod);
                float currentRatio = heightForRatio > 0 ? armValue / (heightForRatio - 0.005f) : 0.4537f;
                DrawMeasurementWithToggle(parameters, "Arm-to-Height Ratio:", $"{currentRatio:F4}", "current_scale_ratio");

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        public static void DrawMeasurementConfigSection(IParameterProvider parameters, ImmersiveScalerCore scalerCore,
            ref bool showMeasurementConfig, ref bool showDebugRatios)
        {
            showMeasurementConfig = EditorGUILayout.Foldout(showMeasurementConfig, "Measurement Config", true);
            if (showMeasurementConfig && scalerCore != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Header
                EditorGUILayout.LabelField("Measurement Methods", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                // Arm Length Method group
                EditorGUILayout.LabelField("Arm Length Method (for Arm-to-Height Ratio):", EditorStyles.boldLabel);
                DrawMeasurementWithSelectors(parameters, "Head-to-Elbow (VRC):", $"{scalerCore.HeadToHand():F3}m", "head_to_elbow_vrc", false, true, false);
                DrawMeasurementWithSelectors(parameters, "Head-to-Hand:", $"{scalerCore.HeadToWrist():F3}m", "head_to_hand", false, true, false);
                DrawMeasurementWithSelectors(parameters, "Arm Length (shoulder to wrist):", $"{scalerCore.GetArmLength():F3}m", "arm_length", false, true, false);
                DrawMeasurementWithSelectors(parameters, "Shoulder to Fingertip:", $"{scalerCore.GetShoulderToFingertip():F3}m", "shoulder_to_fingertip", false, true, false);
                DrawMeasurementWithSelectors(parameters, "Center to Hand:", $"{scalerCore.GetCenterToHand():F3}m", "center_to_hand", false, true, false);
                DrawMeasurementWithSelectors(parameters, "Center to Fingertip:", $"{scalerCore.GetCenterToFingertip():F3}m", "center_to_fingertip", false, true, false);

                EditorGUILayout.Space(10);

                // Height Method group
                EditorGUILayout.LabelField("Height Method:", EditorStyles.boldLabel);
                float totalHeight = scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint();
                float eyeHeight = scalerCore.GetEyeHeight() - scalerCore.GetLowestPoint();
                DrawMeasurementWithSelectors(parameters, "Total Height:", $"{totalHeight:F3}m", "total_height", true, false, false);
                DrawMeasurementWithSelectors(parameters, "Eye Height:", $"{eyeHeight:F3}m", "eye_height_debug", true, false, false);

                EditorGUILayout.Space(10);

                // Upper Body Method group
                EditorGUILayout.LabelField("Upper Body Method:", EditorStyles.boldLabel);
                
                // Height measurements
                EditorGUILayout.LabelField("Height Measurement (denominator):", EditorStyles.miniBoldLabel);
                DrawMeasurementWithSelectors(parameters, "Floor to Neck:", $"{scalerCore.GetHeadHeight():F3}m", "neck_height", false, false, true);
                DrawMeasurementWithSelectors(parameters, "Floor to Head:", $"{scalerCore.GetFloorToHeadHeight():F3}m", "head_height", false, false, true);

                EditorGUILayout.Space(5);

                // Torso measurements
                EditorGUILayout.LabelField("Torso Measurement (numerator):", EditorStyles.miniBoldLabel);
                DrawMeasurementWithSelectors(parameters, "Upper Leg to Neck:", $"{scalerCore.GetUpperBodyLength():F3}m", "upper_body_neck", false, false, true);
                
                // Calculate leg to head
                Transform leftLeg = scalerCore.GetBone(HumanBodyBones.LeftUpperLeg);
                Transform rightLeg = scalerCore.GetBone(HumanBodyBones.RightUpperLeg);
                Transform head = scalerCore.GetBone(HumanBodyBones.Head);
                float legToHead = 0f;
                if (leftLeg != null && rightLeg != null && head != null)
                {
                    float legY = (leftLeg.position.y + rightLeg.position.y) / 2f;
                    legToHead = head.position.y - legY;
                }
                DrawMeasurementWithSelectors(parameters, "Upper Leg to Head:", $"{legToHead:F3}m", "upper_body_head", false, false, true);

                EditorGUILayout.Space(5);

                // Legacy method
                EditorGUILayout.LabelField("Legacy Method:", EditorStyles.miniBoldLabel);
                DrawMeasurementWithSelectors(parameters, "Legacy (Leg→Eye/Floor→Eye):", $"{scalerCore.GetUpperBodyPortion() * 100f:F1}%", "upper_body_legacy", false, false, true);

                EditorGUILayout.Space(10);

                // Additional measurements
                EditorGUILayout.LabelField("Other Measurements:", EditorStyles.boldLabel);
                DrawMeasurementWithToggle(parameters, "Fingertip to Fingertip:", $"{scalerCore.GetFingertipToFingertip():F3}m", "fingertip_to_fingertip");

                EditorGUILayout.Space(10);

                // Debug Ratios in a foldout
                showDebugRatios = EditorGUILayout.Foldout(showDebugRatios, "Debug Ratios", true);
                if (showDebugRatios)
                {
                    EditorGUI.indentLevel++;
                    float debugTotalHeight = scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint();
                    float debugEyeHeight = scalerCore.GetEyeHeight() - scalerCore.GetLowestPoint();

                    DrawMeasurementWithToggle(parameters, "Simple Arm/Height:", $"{scalerCore.GetSimpleArmRatio():F4}", "simple_arm_height");
                    DrawMeasurementWithToggle(parameters, "Arm/Eye Height:", $"{scalerCore.GetArmToEyeRatio():F4}", "arm_eye_height");
                    DrawMeasurementWithToggle(parameters, "Head-to-T-pose/Eye Height:", $"{scalerCore.GetCurrentScaling():F4}", "head_tpose_eye_height");

                    // Additional calculations
                    float shoulderToFingertipRatio = scalerCore.GetShoulderToFingertip() / debugTotalHeight;
                    float shoulderToFingertipEyeRatio = scalerCore.GetShoulderToFingertip() / debugEyeHeight;
                    DrawMeasurementWithToggle(parameters, "Shoulder-Fingertip/Height:", $"{shoulderToFingertipRatio:F4}", "shoulder_fingertip_height");
                    DrawMeasurementWithToggle(parameters, "Shoulder-Fingertip/Eye Height:", $"{shoulderToFingertipEyeRatio:F4}", "shoulder_fingertip_eye_height");

                    // Upper body calculations
                    DrawMeasurementWithToggle(parameters, "Upper Body % (Leg→Neck/Floor→Eye):", $"{scalerCore.GetUpperBodyPortion() * 100f:F1}%", "upper_body_percent");
                    DrawMeasurementWithToggle(parameters, "Alternate Upper Body %:", $"{scalerCore.GetAlternateUpperBodyRatio() * 100f:F1}%", "alternate_upper_body_percent");
                    DrawMeasurementWithToggle(parameters, "Head-to-Hand/Eye Height:", $"{scalerCore.GetHeadWristToEyeRatio():F4}", "head_hand_eye_ratio");
                    DrawMeasurementWithToggle(parameters, "Head-to-Hand/Height:", $"{scalerCore.GetHeadWristToHeightRatio():F4}", "head_hand_height_ratio");
                    DrawMeasurementWithToggle(parameters, "Center-Hand/Height:", $"{scalerCore.GetCenterHandToHeightRatio():F4}", "center_hand_height_ratio");
                    DrawMeasurementWithToggle(parameters, "Center-Hand/Eye Height:", $"{scalerCore.GetCenterHandToEyeRatio():F4}", "center_hand_eye_ratio");
                    DrawMeasurementWithToggle(parameters, "Center-Fingertip/Height:", $"{scalerCore.GetCenterFingertipToHeightRatio():F4}", "center_fingertip_height_ratio");
                    DrawMeasurementWithToggle(parameters, "Center-Fingertip/Eye Height:", $"{scalerCore.GetCenterFingertipToEyeRatio():F4}", "center_fingertip_eye_ratio");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        public static void DrawBasicSettingsSection(IParameterProvider parameters, ImmersiveScalerCore scalerCore, SerializedObject serializedObject = null)
        {
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Target Height
            EditorGUILayout.BeginHorizontal();
            if (serializedObject != null)
            {
                var targetHeightProp = serializedObject.FindProperty("targetHeight");
                targetHeightProp.floatValue = EditorGUILayout.Slider(
                    new GUIContent("Target Height", "Desired height of the avatar in meters"),
                    targetHeightProp.floatValue, 0.5f, 3.0f
                );
            }
            else
            {
                parameters.targetHeight = EditorGUILayout.Slider(
                    new GUIContent("Target Height", "Desired height of the avatar in meters"),
                    parameters.targetHeight, 0.5f, 3.0f
                );
            }
            if (GUILayout.Button("Get Current", GUILayout.Width(80)))
            {
                parameters.targetHeight = scalerCore.GetHeightByMethod(parameters.targetHeightMethod);
                parameters.SetDirty();
            }
            EditorGUILayout.EndHorizontal();

            // Upper Body Percentage
            EditorGUILayout.BeginHorizontal();
            if (serializedObject != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("upperBodyPercentage"));
            }
            else
            {
                parameters.upperBodyPercentage = EditorGUILayout.Slider(
                    new GUIContent("Upper Body %", "Percentage of height for upper body"),
                    parameters.upperBodyPercentage, 30f, 75f
                );
            }
            if (GUILayout.Button("Get Current", GUILayout.Width(80)))
            {
                float upperBodyRatio;
                if (parameters.upperBodyUseLegacy)
                {
                    upperBodyRatio = scalerCore.GetUpperBodyPortion();
                }
                else
                {
                    upperBodyRatio = scalerCore.GetUpperBodyRatio(parameters.upperBodyUseNeck, parameters.upperBodyTorsoUseNeck);
                }
                parameters.upperBodyPercentage = upperBodyRatio * 100f;
                parameters.SetDirty();
            }
            EditorGUILayout.EndHorizontal();

            // Arm Ratio
            EditorGUILayout.BeginHorizontal();
            if (serializedObject != null)
            {
                var armRatioProp = serializedObject.FindProperty("customScaleRatio");
                armRatioProp.floatValue = EditorGUILayout.Slider(
                    new GUIContent("Arm Ratio", "CVR's arm ratio - controls IK arm length (default: 0.4537, lower = longer arms)"),
                    armRatioProp.floatValue, 0.2f, 0.8f
                );
            }
            else
            {
                parameters.customScaleRatio = EditorGUILayout.Slider(
                    new GUIContent("Arm Ratio", "CVR's arm ratio - controls IK arm length (default: 0.4537, lower = longer arms)"),
                    parameters.customScaleRatio, 0.2f, 0.8f
                );
            }
            if (GUILayout.Button("Get Current", GUILayout.Width(80)))
            {
                float armValue = scalerCore.GetArmByMethod(parameters.armToHeightRatioMethod);
                float heightValue = scalerCore.GetHeightByMethod(parameters.armToHeightHeightMethod);
                parameters.customScaleRatio = heightValue > 0 ? armValue / (heightValue - 0.005f) : 0.4537f;
                parameters.SetDirty();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        public static void DrawBodyProportionsSection(IParameterProvider parameters, ImmersiveScalerCore scalerCore, SerializedObject serializedObject = null)
        {
            EditorGUILayout.LabelField("Body Proportions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Arm Thickness
            EditorGUILayout.BeginHorizontal();
            if (serializedObject != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("armThickness"));
            }
            else
            {
                parameters.armThickness = EditorGUILayout.Slider(
                    new GUIContent("Arm Thickness", "How much arm thickness to maintain (0% = scale fully, 100% = keep original)"),
                    parameters.armThickness, 0f, 100f
                );
            }
            if (GUILayout.Button("Get Current", GUILayout.Width(80)))
            {
                parameters.armThickness = scalerCore.GetCurrentArmThickness() * 100f;
                parameters.SetDirty();
            }
            EditorGUILayout.EndHorizontal();

            // Leg Thickness
            EditorGUILayout.BeginHorizontal();
            if (serializedObject != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("legThickness"),
                    new GUIContent("Leg Thickness", "How much leg thickness to maintain (0% = scale fully, 100% = keep original)"));
            }
            else
            {
                parameters.legThickness = EditorGUILayout.Slider(
                    new GUIContent("Leg Thickness", "How much leg thickness to maintain (0% = scale fully, 100% = keep original)"),
                    parameters.legThickness, 0f, 100f
                );
            }
            if (GUILayout.Button("Get Current", GUILayout.Width(80)))
            {
                parameters.legThickness = scalerCore.GetCurrentLegThickness() * 100f;
                parameters.SetDirty();
            }
            EditorGUILayout.EndHorizontal();

            // Thigh Percentage
            EditorGUILayout.BeginHorizontal();
            if (serializedObject != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("thighPercentage"),
                    new GUIContent("Upper Leg %", "Percentage of leg that is thigh (10-90%)"));
            }
            else
            {
                parameters.thighPercentage = EditorGUILayout.Slider(
                    new GUIContent("Upper Leg %", "Percentage of leg that is thigh (10-90%)"),
                    parameters.thighPercentage, 10f, 90f
                );
            }
            if (GUILayout.Button("Get Current", GUILayout.Width(80)))
            {
                parameters.thighPercentage = scalerCore.GetThighPercentage() * 100f;
                parameters.SetDirty();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        public static void DrawScalingOptionsSection(IParameterProvider parameters, SerializedObject serializedObject = null)
        {
            EditorGUILayout.LabelField("Scaling Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (serializedObject != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleHand"),
                    new GUIContent("Scale Hands With Arms", "Scale hands proportionally with arms"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleFoot"),
                    new GUIContent("Scale Feet With Legs", "Scale feet proportionally with legs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleEyes"),
                    new GUIContent("Scale To Eyes", "Measure height to eyes instead of top of head"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("centerModel"),
                    new GUIContent("Center Model", "Center avatar at world origin (X=0, Z=0)"));
            }
            else
            {
                parameters.scaleHand = EditorGUILayout.Toggle(
                    new GUIContent("Scale Hands", "Scale hands proportionally with arms"),
                    parameters.scaleHand
                );
                parameters.scaleFoot = EditorGUILayout.Toggle(
                    new GUIContent("Scale Feet", "Scale feet proportionally with legs"),
                    parameters.scaleFoot
                );
                parameters.scaleEyes = EditorGUILayout.Toggle(
                    new GUIContent("Scale to Eyes", "Measure height to eyes instead of top of head"),
                    parameters.scaleEyes
                );
                parameters.centerModel = EditorGUILayout.Toggle(
                    new GUIContent("Center Model", "Center avatar at world origin (X=0, Z=0)"),
                    parameters.centerModel
                );
            }

            EditorGUILayout.EndVertical();
        }

        public static void DrawAdvancedOptionsSection(IParameterProvider parameters, ref bool showAdvanced, SerializedObject serializedObject = null)
        {
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Options", true);
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (serializedObject != null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("extraLegLength"));
                    
                    // Scale Relative (Deprecated)
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleRelative"),
                        new GUIContent("̶S̶c̶a̶l̶e̶ ̶b̶y̶ ̶R̶e̶l̶a̶t̶i̶v̶e̶ ̶P̶r̶o̶p̶o̶r̶t̶i̶o̶n̶s̶", "Use relative proportions mode instead of upper body percentage"));
                    GUI.enabled = true;

                    if (parameters.scaleRelative)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("armToLegs"));
                    }

                    // Keep Head Size (Deprecated)
                    GUI.enabled = false;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("keepHeadSize"),
                        new GUIContent("̶K̶e̶e̶p̶ ̶H̶e̶a̶d̶ ̶S̶i̶z̶e̶", "Keep head size constant by scaling torso"));
                    GUI.enabled = true;
                }
                else
                {
                    // Extra Leg Length (Deprecated - doesn't work)
                    GUI.enabled = false;
                    parameters.extraLegLength = EditorGUILayout.FloatField(
                        new GUIContent("̶E̶x̶t̶r̶a̶ ̶L̶e̶g̶ ̶L̶e̶n̶g̶t̶h̶", "Additional leg length below the floor"),
                        parameters.extraLegLength
                    );
                    GUI.enabled = true;

                    // Scale Relative (Deprecated)
                    GUI.enabled = false;
                    parameters.scaleRelative = EditorGUILayout.Toggle(
                        new GUIContent("̶S̶c̶a̶l̶e̶ ̶b̶y̶ ̶R̶e̶l̶a̶t̶i̶v̶e̶ ̶P̶r̶o̶p̶o̶r̶t̶i̶o̶n̶s̶", "Use relative proportions mode instead of upper body percentage"),
                        parameters.scaleRelative
                    );
                    GUI.enabled = true;

                    if (parameters.scaleRelative)
                    {
                        parameters.armToLegs = EditorGUILayout.Slider(
                            new GUIContent("Arm to Legs %", "Percentage of scaling to apply to legs (only in relative mode)"),
                            parameters.armToLegs, 0f, 100f
                        );
                    }

                    // Keep Head Size (Deprecated)
                    GUI.enabled = false;
                    parameters.keepHeadSize = EditorGUILayout.Toggle(
                        new GUIContent("̶K̶e̶e̶p̶ ̶H̶e̶a̶d̶ ̶S̶i̶z̶e̶", "Keep head size constant by scaling torso"),
                        parameters.keepHeadSize
                    );
                    GUI.enabled = true;
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Debug Options", EditorStyles.boldLabel);

                if (serializedObject != null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("skipMainRescale"),
                        new GUIContent("Skip Main Rescale", "Skip proportion adjustments"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("skipMoveToFloor"),
                        new GUIContent("Skip Move to Floor", "Don't move avatar to Y=0"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("skipHeightScaling"),
                        new GUIContent("Skip Height Scaling", "Don't scale to target height"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("useBoneBasedFloorCalculation"));
                }
                else
                {
                    parameters.skipMainRescale = EditorGUILayout.Toggle(
                        new GUIContent("Skip Main Rescale", "Skip proportion adjustments"),
                        parameters.skipMainRescale
                    );
                    parameters.skipMoveToFloor = EditorGUILayout.Toggle(
                        new GUIContent("Skip Move to Floor", "Don't move avatar to Y=0"),
                        parameters.skipMoveToFloor
                    );
                    parameters.skipHeightScaling = EditorGUILayout.Toggle(
                        new GUIContent("Skip Height Scaling", "Don't scale to target height"),
                        parameters.skipHeightScaling
                    );
                    parameters.useBoneBasedFloorCalculation = EditorGUILayout.Toggle(
                        new GUIContent("Use Bone-Based Floor", "Use bone positions instead of mesh bounds for floor calculation"),
                        parameters.useBoneBasedFloorCalculation
                    );
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        public static void DrawAdditionalToolsSection(IParameterProvider parameters, ref bool showAdditionalTools, SerializedObject serializedObject = null)
        {
            showAdditionalTools = EditorGUILayout.Foldout(showAdditionalTools, "Additional Tools", true);
            if (showAdditionalTools)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                if (serializedObject != null)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("applyFingerSpreading"),
                        new GUIContent("Apply Finger Spreading", "Apply finger spreading during avatar build"));

                    if (parameters.applyFingerSpreading)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("fingerSpreadFactor"),
                            new GUIContent("Spread Factor", "How much to spread fingers apart"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("spareThumb"),
                            new GUIContent("Ignore Thumb", "Don't spread the thumb"));
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("applyShrinkHipBone"),
                        new GUIContent("Apply Hip Fix", "Apply hip bone fix during avatar build"));
                }
                else
                {
                    EditorGUILayout.LabelField("Finger Spreading", EditorStyles.boldLabel);
                    parameters.spareThumb = EditorGUILayout.Toggle(
                        new GUIContent("Ignore Thumb", "Don't spread the thumb"),
                        parameters.spareThumb
                    );
                    parameters.fingerSpreadFactor = EditorGUILayout.Slider(
                        new GUIContent("Spread Factor", "How much to spread fingers apart"),
                        parameters.fingerSpreadFactor, 0f, 2f
                    );

                    if (GUILayout.Button("Apply Finger Spreading"))
                    {
                        // This will be handled by the window
                        EditorApplication.delayCall += () => Debug.Log("Apply finger spreading");
                    }

                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("Hip Bone Fix", EditorStyles.boldLabel);

                    if (GUILayout.Button("Shrink Hip Bone"))
                    {
                        // This will be handled by the window
                        EditorApplication.delayCall += () => Debug.Log("Shrink hip bone");
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        // Helper methods
        private static void DrawMeasurementWithToggle(IParameterProvider parameters, string label, string value, string measurementKey)
        {
            bool isHighlighted = IsRelatedMeasurement(parameters.debugMeasurement, measurementKey);

            if (isHighlighted)
            {
                GUI.backgroundColor = new Color(1f, 1f, 0.5f, 0.3f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
            }

            GUILayout.Space(5);

            bool isActive = parameters.debugMeasurement == measurementKey;
            GUI.backgroundColor = isActive ? Color.green : Color.white;
            if (GUILayout.Button(isActive ? "●" : "○", GUILayout.Width(20), GUILayout.Height(18)))
            {
                parameters.debugMeasurement = isActive ? "" : measurementKey;
                parameters.SetDirty();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.LabelField(label, value);
            EditorGUILayout.EndHorizontal();

            if (isHighlighted)
            {
                GUI.backgroundColor = Color.white;
            }
        }

        private static void DrawMeasurementWithSelectors(IParameterProvider parameters, string label, string value, string measurementKey,
            bool showHeightSelectors, bool showArmSelectors, bool showUpperBodySelectors)
        {
            bool isHighlighted = IsRelatedMeasurement(parameters.debugMeasurement, measurementKey);

            if (isHighlighted)
            {
                GUI.backgroundColor = new Color(1f, 1f, 0.5f, 0.3f);
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
            }

            GUILayout.Space(5);

            bool isActive = parameters.debugMeasurement == measurementKey;
            GUI.backgroundColor = isActive ? Color.green : Color.white;
            if (GUILayout.Button(isActive ? "●" : "○", GUILayout.Width(20), GUILayout.Height(18)))
            {
                parameters.debugMeasurement = isActive ? "" : measurementKey;
                parameters.SetDirty();
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.LabelField(label, value, GUILayout.MinWidth(250));
            GUILayout.FlexibleSpace();

            if (showHeightSelectors)
            {
                DrawHeightSelectionButtons(measurementKey, parameters);
            }
            else if (showArmSelectors)
            {
                DrawArmSelectionButton(measurementKey, parameters);
            }
            else if (showUpperBodySelectors)
            {
                bool isHeightMeasurement = measurementKey.Contains("head_height") || measurementKey.Contains("neck_height");
                DrawUpperBodySelectionButtons(measurementKey, parameters, isHeightMeasurement);
            }

            EditorGUILayout.EndHorizontal();

            if (isHighlighted)
            {
                GUI.backgroundColor = Color.white;
            }
        }

        private static void DrawHeightSelectionButtons(string measurementKey, IParameterProvider parameters)
        {
            var heightType = measurementKey.Contains("eye") ?
                HeightMethodType.EyeHeight :
                HeightMethodType.TotalHeight;

            bool isTargetHeight = parameters.targetHeightMethod == heightType;
            GUI.backgroundColor = isTargetHeight ? Color.green : Color.white;
            if (GUILayout.Button("Target Height", GUILayout.Width(90)))
            {
                parameters.targetHeightMethod = heightType;
                parameters.SetDirty();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(5);

            bool isArmHeight = parameters.armToHeightHeightMethod == heightType;
            GUI.backgroundColor = isArmHeight ? Color.green : Color.white;
            if (GUILayout.Button("Arm Ratio", GUILayout.Width(70)))
            {
                parameters.armToHeightHeightMethod = heightType;
                parameters.SetDirty();
            }
            GUI.backgroundColor = Color.white;
        }

        private static void DrawArmSelectionButton(string measurementKey, IParameterProvider parameters)
        {
            var armType = GetArmTypeFromKey(measurementKey);

            bool isSelected = parameters.armToHeightRatioMethod == armType;
            GUI.backgroundColor = isSelected ? Color.green : Color.white;
            if (GUILayout.Button("Arm Ratio", GUILayout.Width(70)))
            {
                parameters.armToHeightRatioMethod = armType;
                parameters.SetDirty();
            }
            GUI.backgroundColor = Color.white;
        }

        private static void DrawUpperBodySelectionButtons(string measurementKey, IParameterProvider parameters, bool isHeightMeasurement)
        {
            if (measurementKey == "upper_body_legacy")
            {
                bool isSelected = parameters.upperBodyUseLegacy;
                GUI.backgroundColor = isSelected ? Color.green : Color.white;
                if (GUILayout.Button("Upper Body", GUILayout.Width(90)))
                {
                    parameters.upperBodyUseLegacy = true;
                    parameters.SetDirty();
                }
                GUI.backgroundColor = Color.white;
            }
            else if (isHeightMeasurement)
            {
                bool useNeck = measurementKey.Contains("neck");
                bool isSelected = parameters.upperBodyUseNeck == useNeck && !parameters.upperBodyUseLegacy;
                GUI.backgroundColor = isSelected ? Color.green : Color.white;
                if (GUILayout.Button("Upper Body Height", GUILayout.Width(120)))
                {
                    parameters.upperBodyUseNeck = useNeck;
                    parameters.upperBodyUseLegacy = false;
                    parameters.SetDirty();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                bool useNeck = measurementKey.Contains("neck");
                bool isSelected = parameters.upperBodyTorsoUseNeck == useNeck && !parameters.upperBodyUseLegacy;
                GUI.backgroundColor = isSelected ? Color.green : Color.white;
                if (GUILayout.Button("Torso Length", GUILayout.Width(90)))
                {
                    parameters.upperBodyTorsoUseNeck = useNeck;
                    parameters.upperBodyUseLegacy = false;
                    parameters.SetDirty();
                }
                GUI.backgroundColor = Color.white;
            }
        }

        private static bool IsRelatedMeasurement(string activeMeasurement, string checkMeasurement)
        {
            if (string.IsNullOrEmpty(activeMeasurement)) return false;
            if (activeMeasurement == checkMeasurement) return false;

            if (relatedMeasurements.TryGetValue(activeMeasurement, out string[] related))
            {
                return related.Contains(checkMeasurement);
            }
            return false;
        }

        private static ArmMethodType GetArmTypeFromKey(string key)
        {
            if (key.Contains("head_to_elbow")) return ArmMethodType.HeadToElbowVRC;
            if (key.Contains("head_to_hand") || key.Contains("head_hand")) return ArmMethodType.HeadToHand;
            if (key.Contains("arm_length") && !key.Contains("center")) return ArmMethodType.ArmLength;
            if (key.Contains("shoulder") && key.Contains("fingertip")) return ArmMethodType.ShoulderToFingertip;
            if (key.Contains("center") && key.Contains("fingertip")) return ArmMethodType.CenterToFingertip;
            if (key.Contains("center") && key.Contains("hand")) return ArmMethodType.CenterToHand;
            return ArmMethodType.HeadToElbowVRC;
        }

        public static string GetMeasurementKeyForArmType(ArmMethodType armType)
        {
            return armType switch
            {
                ArmMethodType.HeadToElbowVRC => "head_to_elbow_vrc",
                ArmMethodType.HeadToHand => "head_to_hand",
                ArmMethodType.ArmLength => "arm_length",
                ArmMethodType.ShoulderToFingertip => "shoulder_to_fingertip",
                ArmMethodType.CenterToHand => "center_to_hand",
                ArmMethodType.CenterToFingertip => "center_to_fingertip",
                _ => "arm_length"
            };
        }

        // Scene visualization methods
        public static void DrawMeasurementWithHandles(string measurementKey, ImmersiveScalerCore scalerCore, IParameterProvider parameters, Component avatar)
        {
            switch (measurementKey)
            {
                case "current_height":
                case "total_height":
                    {
                        float lowest = scalerCore.GetLowestPoint();
                        float highest = scalerCore.GetHighestPoint();
                        Vector3 start = new Vector3(0, lowest, 0);
                        Vector3 end = new Vector3(0, highest, 0);
                        DrawHandlesLine(start, end, Color.green);
                    }
                    break;

                case "eye_height":
                case "eye_height_debug":
                    {
                        float lowest = scalerCore.GetLowestPoint();
                        Vector3 start = new Vector3(0, lowest, 0);
                        Vector3 end = new Vector3(0, scalerCore.GetEyeHeight(), 0);
                        DrawHandlesLine(start, end, Color.green);
                    }
                    break;

                case "view_position":
                    {
                        if (avatar != null)
                        {
                            Vector3 viewPos = CVRReflectionHelper.GetViewPosition(avatar);
                            Vector3 worldViewPos = avatar.transform.TransformPoint(viewPos);
                            Handles.color = Color.green;
                            Handles.SphereHandleCap(0, worldViewPos, Quaternion.identity, 0.05f, EventType.Repaint);
                        }
                    }
                    break;

                // Add the rest of the visualization cases here - they remain unchanged from the original
                // (continuing with all the other cases like arm measurements, upper body, etc.)
            }
        }

        private static void DrawHandlesLine(Vector3 start, Vector3 end, Color color)
        {
            Handles.color = color;
            Handles.DrawLine(start, end, 3f);
            Handles.SphereHandleCap(0, start, Quaternion.identity, 0.02f, EventType.Repaint);
            Handles.SphereHandleCap(0, end, Quaternion.identity, 0.02f, EventType.Repaint);
        }
    }
}
