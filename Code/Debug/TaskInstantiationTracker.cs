using HTN.Tasks;
using System;
using System.Collections.Generic;

namespace HTN.Debug;

/// <summary>
/// DEBUG-time task tracking to help identify designer problems given we can't prevent direct instantiation of tasks,
/// garbage collection is significant when instantiating tasks per-plan with thousands of active agents so pooling and using Run&lt;TaskType&gt;() is important.
/// </summary>
public static class TaskInstantiationTracker
{
	private static readonly HashSet<ITask> _poolCreatedTasks = [];
    private static readonly Dictionary<Type, int> _directInstantiations = [];

	public static void RegisterPoolCreation(ITask task)
    {
        if (task == null) return;
        
        lock (_poolCreatedTasks)
        {
            _poolCreatedTasks.Add(task);
        }
    }
    
    public static bool IsPoolCreated(ITask task)
    {
        if (task == null) return false;
        
        lock (_poolCreatedTasks)
        {
            return _poolCreatedTasks.Contains(task);
        }
    }
    
    public static void ReportDirectInstantiation(ITask task)
    {
        if (task == null)
		{
			throw new ArgumentNullException(nameof(task));
		}

		var type = task.GetType();
        lock (_directInstantiations)
        {
            _directInstantiations.TryGetValue(type, out var count);
            _directInstantiations[type] = count + 1;
        }
        
        Log.Warning($"Run {type.Name} was created directly, not through TaskPool. " +
                   $"Use Run<{type.Name}>() instead of new {type.Name}(). " +
                   $"Total direct instantiations of this type: {_directInstantiations[type]}");
    }
    
    public static void ValidatePoolUsage(IEnumerable<ITask> tasks, string context = "")
    {
        if (tasks == null) return;
        
        foreach (var task in tasks)
        {
            if (task == null) continue;
            
            if (!IsPoolCreated(task))
            {
                Log.Warning($"Non-pool task detected in {context}: {task.GetType().Name}");
                ReportDirectInstantiation(task);
            }
        }
    }
    
    public static Dictionary<Type, int> GetDirectInstantiationStats()
    {
        lock (_directInstantiations)
        {
            return new Dictionary<Type, int>(_directInstantiations);
        }
    }
    
    public static void Clear()
    {
        lock (_poolCreatedTasks)
        {
            _poolCreatedTasks.Clear();
        }
        lock (_directInstantiations)
        {
            _directInstantiations.Clear();
        }
    }
}
