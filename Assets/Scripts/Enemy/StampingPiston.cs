using UnityEngine;

public class StampingPiston : MonoBehaviour
{
    public enum MoveAxis
    {
        X,
        Y,
        Z
    }

    [Header("Movement Axis")]
    public MoveAxis moveAxis = MoveAxis.Y;

    [Header("Distances")]

    public float strokeDistance = 2f;

    [Tooltip("回弹距离 = 冲程 × 比例")]
    [Range(0f, 0.5f)]
    public float bounceRatio = 0.08f;

    [Header("Timing")]

    public float impactTime = 0.2f;
    public float returnTime = 0.8f;

    public float bounceTime = 0.05f;

    public float waitTime = 0.25f;
    public float cooldownTime = 1f;

    public float startDelay = 0f;

    [Header("Motion Curves")]

    public AnimationCurve impactCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    public AnimationCurve returnCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 startPos;
    private Vector3 targetPos;
    private Vector3 moveDir;

    private Vector3 bounceStartPos;
    private Vector3 bounceTargetPos;

    private float timer = 0f;

    private enum State
    {
        StartDelay,
        Impact,
        BounceBack,
        BounceForward,
        Wait,
        Return,
        Cooldown
    }

    private State currentState;

    void Start()
    {
        startPos = transform.localPosition;

        moveDir = GetLocalDirection();

        targetPos =
            startPos +
            moveDir * strokeDistance;

        if (startDelay > 0f)
        {
            currentState = State.StartDelay;
            timer = startDelay;
        }
        else
        {
            currentState = State.Impact;
            timer = 0f;
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.StartDelay:

                timer -= Time.deltaTime;

                if (timer <= 0f)
                {
                    currentState = State.Impact;
                    timer = 0f;
                }

                break;

            case State.Impact:

                CurveMove(
                    startPos,
                    targetPos,
                    impactTime,
                    impactCurve,
                    State.BounceBack
                );

                break;

            case State.BounceBack:

                BounceMove(
                    true,
                    State.BounceForward
                );

                break;

            case State.BounceForward:

                BounceMove(
                    false,
                    State.Wait
                );

                break;

            case State.Wait:

                timer -= Time.deltaTime;

                if (timer <= 0f)
                {
                    currentState = State.Return;
                    timer = 0f;
                }

                break;

            case State.Return:

                CurveMove(
                    targetPos,
                    startPos,
                    returnTime,
                    returnCurve,
                    State.Cooldown
                );

                break;

            case State.Cooldown:

                timer -= Time.deltaTime;

                if (timer <= 0f)
                {
                    currentState = State.Impact;
                    timer = 0f;
                }

                break;
        }
    }

    void CurveMove(
        Vector3 from,
        Vector3 to,
        float duration,
        AnimationCurve curve,
        State nextState
    )
    {
        timer += Time.deltaTime;

        float t =
            Mathf.Clamp01(timer / duration);

        float curveValue =
            curve.Evaluate(t);

        transform.localPosition =
            Vector3.Lerp(
                from,
                to,
                curveValue
            );

        if (t >= 1f)
        {
            timer = 0f;

            if (nextState == State.BounceBack)
            {
                SetupBounce(true);
            }

            if (nextState == State.Cooldown)
            {
                timer = cooldownTime;
            }

            currentState = nextState;
        }
    }

    void BounceMove(
        bool isBack,
        State nextState
    )
    {
        timer += Time.deltaTime;

        float t =
            Mathf.Clamp01(timer / bounceTime);

        transform.localPosition =
            Vector3.Lerp(
                bounceStartPos,
                bounceTargetPos,
                t
            );

        if (t >= 1f)
        {
            timer = 0f;

            if (nextState == State.BounceForward)
            {
                SetupBounce(false);
            }

            if (nextState == State.Wait)
            {
                timer = waitTime;
            }

            currentState = nextState;
        }
    }

    void SetupBounce(bool isBack)
    {
        float bounceDistance =
            strokeDistance * bounceRatio;

        bounceStartPos =
            transform.localPosition;

        bounceTargetPos =
            isBack
            ? targetPos - moveDir * bounceDistance
            : targetPos;
    }

    Vector3 GetLocalDirection()
    {
        switch (moveAxis)
        {
            case MoveAxis.X:
                return Vector3.right;

            case MoveAxis.Y:
                return Vector3.up;

            case MoveAxis.Z:
                return Vector3.forward;
        }

        return Vector3.up;
    }

#if UNITY_EDITOR

    void OnValidate()
    {
        moveDir = GetLocalDirection();

        startPos = transform.localPosition;

        targetPos =
            startPos +
            moveDir * strokeDistance;
    }

#endif
}