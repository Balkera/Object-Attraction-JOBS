using System.Collections.Generic;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;

public class AttractObjects : MonoBehaviour
{
    public static List<Transform> _objectPosList = new List<Transform>();
    [SerializeField] private DataBank scoreManagerSO;
    private TransformAccessArray _transformAccessArray;
    private AttractJob _job;
    private JobHandle _jobHandle;
    [SerializeField] private Transform _playerTransform;
    public static bool disableAttraction;
    [SerializeField] private float attractionMultiplier = 1, speedMultiplier = 1;

    private void Start()
    {
        
        _transformAccessArray = new TransformAccessArray(0, -1);
    }

    private void OnDisable()
    {
        _jobHandle.Complete();
        if (_transformAccessArray.isCreated)
        {
            _transformAccessArray.Dispose();
        }
    }

    private void Update()
    {
        if (_playerTransform == null && MoneyController.MoneyControllerInstance != null)
        {
            _playerTransform = MoneyController.MoneyControllerInstance.transform;
        }

        if (_objectPosList.Count > 0)
        {
            if (!_transformAccessArray.isCreated)
            {
                _transformAccessArray = new TransformAccessArray(_objectPosList.Count, -1);
                _transformAccessArray.SetTransforms(_objectPosList.ToArray());

            }
            else if (_transformAccessArray.length != _objectPosList.Count)
            {
                _transformAccessArray.capacity = _objectPosList.Count;
                _transformAccessArray.SetTransforms(_objectPosList.ToArray());
            }
        }

        if (_transformAccessArray.isCreated && _transformAccessArray.length > _objectPosList.Count)
        {
            _transformAccessArray.RemoveAtSwapBack(0);
        }

        if (_transformAccessArray.isCreated && _transformAccessArray.length > 0)
        {
            if (!disableAttraction)
            {
                UseJob();
            }
        }

        if (_transformAccessArray.isCreated && _transformAccessArray.length == 0)
        {
            _jobHandle.Complete();
            _transformAccessArray.Dispose();
        }

    }
    private void UseJob()
    {
        float deltaTime = Time.deltaTime;
        _job = new AttractJob
        {
            _playerPos = _playerTransform.position,
            _attractDistance = scoreManagerSO.attractDistance * attractionMultiplier,
            _speed = scoreManagerSO.attractSpeed * speedMultiplier,
            deltatime = deltaTime
        };

        // Schedule the job for execution
        _jobHandle = _job.Schedule(_transformAccessArray);

        // Ensure the job is completed before accessing the data
        _jobHandle.Complete();
    }
    [BurstCompile]
    public struct AttractJob : IJobParallelForTransform
    {
        [ReadOnly] public float3 _playerPos;
        [ReadOnly] public float _attractDistance;
        [ReadOnly] public float _speed;
        [ReadOnly] public float deltatime;

        public void Execute(int index, TransformAccess transform)
        {
            float3 direction = math.distance(_playerPos, (float3)transform.position);
            float distance = math.length(direction);
            bool shouldPerformAttraction = _attractDistance > distance;

            if (shouldPerformAttraction)
            {
                transform.position = Vector3.Lerp(transform.position, _playerPos, deltatime * _speed);
            }

        }
    }
}

