using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class PenguinAgent : Agent
{
    [Tooltip("How fast the agent moves forward")]
    public float moveSpeed = 5f;

    [Tooltip("How fast the agent turns")]
    public float turnSpeed = 180f;

    [Tooltip("Prefab of the heart that appears when the baby is fed")]
    public GameObject heartPrefab;

    [Tooltip("Prefab of the regurgitated fish that appears when the baby is fed")]
    public GameObject regurgitatedFishPrefab;

    private PenguinArea penguinArea;
    new private Rigidbody rigidbody;
    private GameObject baby;
    private bool isFull;
    private float feedRadius = 0f;

    public override void InitializeAgent()
    {
        base.InitializeAgent();
        penguinArea = GetComponentInParent<PenguinArea>();
        baby = penguinArea.penguinBaby;
        rigidbody = GetComponent<Rigidbody>();
    }

    public override void AgentAction(float[] vectorAction)
    {
        float forwardAmount = vectorAction[0];

        float turnAmount = 0f;
        if(vectorAction[1] == 1f)
        {
            turnAmount = -1f;
        }
        else if (vectorAction[1] == 2f)
        {
            turnAmount = 1f;
        }

        rigidbody.MovePosition(transform.position + transform.forward * forwardAmount * moveSpeed * Time.fixedDeltaTime);
        transform.Rotate(transform.up * turnAmount * turnSpeed * Time.fixedDeltaTime);

        if (maxStep > 0) AddReward(-1f / maxStep);
    }

    public override float[] Heuristic()
    {
        float forwardAction = 0f;
        float turnAction = 0f;
        if(Input.GetKey(KeyCode.W))
        {
            forwardAction = 1f;
        }
        if(Input.GetKey(KeyCode.A))
        {
            turnAction = 1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            turnAction = 2f;
        }

        return new float[] { forwardAction, turnAction };
    }

    public override void AgentReset()
    {
        isFull = false;
        penguinArea.ResetArea();
        feedRadius = Academy.Instance.FloatProperties.GetPropertyWithDefault("feed_radius", 0f);
    }

    public override void CollectObservations()
    {
        AddVectorObs(isFull);

        AddVectorObs(Vector3.Distance(baby.transform.position, transform.position));

        AddVectorObs((baby.transform.position - transform.position).normalized);

        AddVectorObs(transform.forward);

        for (int i = 0; i < penguinArea.allPenguins.Length; i++)
        {
            if (penguinArea.allPenguins[i] != this)
            {
                AddVectorObs(Vector3.Distance(penguinArea.allPenguins[i].transform.position, transform.position));
                AddVectorObs((penguinArea.allPenguins[i].transform.position - transform.position).normalized);
            }

        }
    }

    private void FixedUpdate()
    {
        if(GetStepCount() % 5 == 0)
        {
            RequestDecision();
        }
        else
        {
            RequestAction();
        }

        if(Vector3.Distance(transform.position,baby.transform.position) < feedRadius)
        {
            RegurgitateFish();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.CompareTag("fish"))
        {
            EatFish(collision.gameObject);
        }
        else if (collision.transform.CompareTag("penguin"))
        {
            AddReward(-2f);
        }
        else if(collision.transform.CompareTag("baby"))
        {
            RegurgitateFish();
        }

    }

    private void EatFish(GameObject fishObject)
    {
        if (isFull) return;
        isFull = true;

        penguinArea.RemoveSpecificFish(fishObject);

        AddReward(1f);
    }

    private void RegurgitateFish()
    {
        if (!isFull) return;
        isFull = false;

        GameObject regurgitatedFish = Instantiate<GameObject>(regurgitatedFishPrefab);
        regurgitatedFish.transform.parent = transform.parent;
        regurgitatedFish.transform.position = baby.transform.position;
        Destroy(regurgitatedFish, 4f);

        GameObject heart = Instantiate<GameObject>(heartPrefab);
        heart.transform.parent = transform.parent;
        heart.transform.position = baby.transform.position + Vector3.up;
        Destroy(heart, 4f);

        AddReward(1f);

        if(penguinArea.FishRemaining <= 0)
        {
            Done();
        }
    }
}
