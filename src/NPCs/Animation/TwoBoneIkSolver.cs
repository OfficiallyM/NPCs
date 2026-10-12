using UnityEngine;

namespace NPCs.Animation
{
    public static class TwoBoneIkSolver
    {
        private const float Epsilon = 0.0001f;

        public static bool Solve(
            Transform root,
            Transform middle,
            Transform tip,
            Vector3 targetPosition,
            Vector3 polePosition,
            float weight)
        {
            if (root == null || middle == null || tip == null)
                return false;

            weight = Mathf.Clamp01(weight);
            if (weight <= 0f)
                return false;

            Vector3 rootPosition = root.position;
            Vector3 middlePosition = middle.position;
            float upperLength = Vector3.Distance(rootPosition, middlePosition);
            float lowerLength = Vector3.Distance(middlePosition, tip.position);

            if (upperLength <= Epsilon || lowerLength <= Epsilon)
                return false;

            Vector3 targetOffset = targetPosition - rootPosition;
            float targetDistance = targetOffset.magnitude;
            if (targetDistance <= Epsilon)
                return false;

            float minimumReach = Mathf.Abs(upperLength - lowerLength) + Epsilon;
            float maximumReach = upperLength + lowerLength - Epsilon;
            if (maximumReach <= minimumReach)
                return false;

            Vector3 targetDirection = targetOffset / targetDistance;
            float reachDistance = Mathf.Clamp(targetDistance, minimumReach, maximumReach);
            Vector3 reachableTarget = rootPosition + targetDirection * reachDistance;

            Vector3 bendDirection = Vector3.ProjectOnPlane(
                polePosition - rootPosition,
                targetDirection);

            if (bendDirection.sqrMagnitude <= Epsilon * Epsilon)
            {
                bendDirection = Vector3.ProjectOnPlane(
                    middlePosition - rootPosition,
                    targetDirection);
            }

            if (bendDirection.sqrMagnitude <= Epsilon * Epsilon)
            {
                Vector3 fallback = Mathf.Abs(Vector3.Dot(targetDirection, Vector3.up)) < 0.95f
                    ? Vector3.up
                    : Vector3.forward;

                bendDirection = Vector3.ProjectOnPlane(fallback, targetDirection);
            }

            if (bendDirection.sqrMagnitude <= Epsilon * Epsilon)
                return false;

            bendDirection.Normalize();

            float along = (upperLength * upperLength +
				reachDistance * reachDistance -
				lowerLength * lowerLength)
				/ (2f * reachDistance);

            float height = Mathf.Sqrt(Mathf.Max(
                0f,
                upperLength * upperLength - along * along
			));

            Vector3 desiredMiddlePosition = rootPosition
                + targetDirection * along
                + bendDirection * height;

            Vector3 currentUpperDirection = middle.position - root.position;
            Vector3 desiredUpperDirection = desiredMiddlePosition - root.position;

            if (currentUpperDirection.sqrMagnitude <= Epsilon * Epsilon || desiredUpperDirection.sqrMagnitude <= Epsilon * Epsilon)
                return false;

            Quaternion upperCorrection = Quaternion.FromToRotation(
                currentUpperDirection,
                desiredUpperDirection
			);

            root.rotation = Quaternion.Slerp(
                root.rotation,
                upperCorrection * root.rotation,
                weight
			);

            Vector3 currentLowerDirection = tip.position - middle.position;
            Vector3 desiredLowerDirection = reachableTarget - middle.position;

            if (currentLowerDirection.sqrMagnitude <= Epsilon * Epsilon || desiredLowerDirection.sqrMagnitude <= Epsilon * Epsilon)
                return false;

            Quaternion lowerCorrection = Quaternion.FromToRotation(
                currentLowerDirection,
                desiredLowerDirection
			);

            middle.rotation = Quaternion.Slerp(
                middle.rotation,
                lowerCorrection * middle.rotation,
                weight
			);

            return true;
        }
    }
}
