using System.Reflection;
using nadena.dev.ndmf;
using UnityEngine;
using UnityEditor;

[assembly: ExportsPlugin(typeof(VRChatImmersiveScaler.Editor.ImmersiveScalerPlugin))]

namespace VRChatImmersiveScaler.Editor
{
    public class ImmersiveScalerPlugin : Plugin<ImmersiveScalerPlugin>
    {
        public override string QualifiedName => "com.vrchat.immersivescaler";
        public override string DisplayName => "ChilloutVR Immersive Scaler";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .Run("Apply Immersive Scaling", ctx =>
                {
                    var component = ctx.AvatarRootTransform.GetComponentInChildren<ImmersiveScalerComponent>(true);
                    if (component == null) return;

                    // Get CVRAvatar via reflection
                    var descriptor = CVRReflectionHelper.GetCVRAvatar(ctx.AvatarRootTransform.gameObject);
                    if (descriptor == null)
                    {
                        Debug.LogError("[ImmersiveScaler] No CVRAvatar found!");
                        return;
                    }

                    Debug.Log($"[ImmersiveScaler] Starting scaling process. Target height: {component.targetHeight}m");

                    // Store original positions for logging only
                    Vector3 originalViewPosition = CVRReflectionHelper.GetViewPosition(descriptor);
                    Vector3 originalVoicePosition = CVRReflectionHelper.GetVoicePosition(descriptor);
                    
                    Debug.Log($"[ImmersiveScaler] Original viewPosition: {originalViewPosition}");
                    Debug.Log($"[ImmersiveScaler] Original voicePosition: {originalVoicePosition}");

                    // Create scaling core
                    var scalerCore = new ImmersiveScalerCore(ctx.AvatarRootTransform.gameObject);

                    // Store original avatar scale
                    Vector3 originalAvatarScale = ctx.AvatarRootTransform.localScale;

                    // Create parameters from component
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

                    // Apply scaling
                    scalerCore.ScaleAvatar(parameters);

                    // Calculate scale ratio
                    Vector3 newAvatarScale = ctx.AvatarRootTransform.localScale;
                    float scaleRatio = newAvatarScale.y / originalAvatarScale.y;

                    Debug.Log($"[ImmersiveScaler] Avatar scale changed from {originalAvatarScale} to {newAvatarScale}");
                    Debug.Log($"[ImmersiveScaler] Scale ratio: {scaleRatio}");

                    // DON'T manually scale viewPosition/voicePosition
                    // CVRAvatar's CheckScaleViewAndVoicePositions() will handle this automatically
                    // when it detects the scale change in the next Update() cycle
                    
                    // Instead, just force an editor update to trigger CVRAvatar's auto-scaling
                    EditorUtility.SetDirty(descriptor);

                    // Log what the positions should become
                    Vector3 expectedViewPosition = originalViewPosition * scaleRatio;
                    Vector3 expectedVoicePosition = originalVoicePosition * scaleRatio;
                    Debug.Log($"[ImmersiveScaler] Expected viewPosition after CVRAvatar auto-scale: {expectedViewPosition}");
                    Debug.Log($"[ImmersiveScaler] Expected voicePosition after CVRAvatar auto-scale: {expectedVoicePosition}");

                    float finalHeight = scalerCore.GetHighestPoint() - scalerCore.GetLowestPoint();
                    Debug.Log($"[ImmersiveScaler] Scaling complete. Final height: {finalHeight:F3}m (target was {component.targetHeight:F3}m)");

                    // Apply additional tools
                    if (component.applyFingerSpreading)
                    {
                        Debug.Log($"[ImmersiveScaler] Applying finger spreading with factor {component.fingerSpreadFactor}");
                        ImmersiveScalerFingerUtility.SpreadFingers(ctx.AvatarRootTransform.gameObject, 
                            component.fingerSpreadFactor, component.spareThumb);
                    }

                    if (component.applyShrinkHipBone)
                    {
                        Debug.Log("[ImmersiveScaler] Applying hip bone fix");
                        ApplyHipBoneFix(ctx.AvatarRootTransform.gameObject);
                    }

                    // Remove component after processing
                    Object.DestroyImmediate(component);
                    
                    Debug.Log("[ImmersiveScaler] ✓ Processing complete - CVRAvatar will auto-scale viewPosition and voicePosition");
                });
        }

        private static void ApplyHipBoneFix(GameObject avatar)
        {
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null || !animator.isHuman) return;

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform leftLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform rightLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);

            if (hips == null || spine == null || leftLeg == null || rightLeg == null)
            {
                Debug.LogError("[ImmersiveScaler] Cannot find required bones for hip shrinking");
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
