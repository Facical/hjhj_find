using UnityEngine;
using UnityEngine.AI; // 이게 있어야 내비게이션을 씁니다.

public class SimpleMove : MonoBehaviour
{
    public Transform target; // 목적지
    private NavMeshAgent agent; // 나(캐릭터)

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (target != null)
        {
            agent.SetDestination(target.position); // 목적지로 출발!
        }
    }
}