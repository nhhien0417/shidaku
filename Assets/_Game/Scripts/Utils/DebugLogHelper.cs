using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MEC;
using UnityEngine;

namespace Game.Utils
{
    public class DebugLogHelper
    {
        private static DebugLogHelper _instance;
        private static DebugLogHelper Instance
        {
            get
            {
                _instance ??= new DebugLogHelper();
                return _instance;
            }
        }
        
        private ILogHandler _originalLogHandler;
        private LogPauser _logPauser;
        private CoroutineHandle _autoResumeCoroutine;
        
        public static void PauseLogging(float timeout = -1, bool printPrevLogsOnTimeout = true, Action onTimeout = null)
        {
            Timing.KillCoroutines(Instance._autoResumeCoroutine);
            if (timeout > 0)
                Instance._autoResumeCoroutine = Timing.RunCoroutine(Instance.AutoResumeAfterDelay(timeout, printPrevLogsOnTimeout, onTimeout));
            
            if (Instance._logPauser != null)
                return;
            
            Instance._logPauser = new LogPauser();
            Instance._originalLogHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = Instance._logPauser;
        }

        public static void ResumeLogging(bool printPrevLogs)
        {
            Timing.KillCoroutines(Instance._autoResumeCoroutine);
            
            if (printPrevLogs)
                Instance.PrintAllQueuedLogs();
            
            Debug.unityLogger.logHandler = Instance._originalLogHandler;
            Instance._logPauser?.QueuedLogs.Clear();
            Instance._logPauser = null;
            Instance._originalLogHandler = null;
        }
        
        private void PrintAllQueuedLogs()
        {
            if (_logPauser == null || _originalLogHandler == null)
                return;
            
            _originalLogHandler.LogFormat(LogType.Log, null, "[DebugLogHelper] --- LOGGING RESUMED. Printing {0} queued logs ---", _logPauser.QueuedLogs.Count);
            
            var prefix = "[Delayed] ";
            while (_logPauser.QueuedLogs.Count > 0)
            {
                var log = _logPauser.QueuedLogs.Dequeue();
                _originalLogHandler.LogFormat(log.logType == LogType.Exception ? LogType.Error : log.logType, log.context, prefix + log.message);
            }
        }

        private IEnumerator<float> AutoResumeAfterDelay(float delay, bool printPrevLogsOnTimeout, Action onTimeout)
        {
            yield return Timing.WaitForSeconds(delay);
            ResumeLogging(printPrevLogsOnTimeout);
            onTimeout?.Invoke();
        }

        public class LogPauser : ILogHandler
        {
            private const int _maxQueuedLogs = 1500;
            public Queue<LogMessage> QueuedLogs = new ();
            
            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (QueuedLogs.Count >= _maxQueuedLogs)
                    QueuedLogs.Dequeue();
                
                QueuedLogs.Enqueue(new LogMessage 
                { 
                    logType = logType, 
                    message = args is { Length: > 0 } ? string.Format(format, args) : format, 
                    context = context 
                });
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
                if (QueuedLogs.Count >= _maxQueuedLogs)
                    QueuedLogs.Dequeue();
                
                QueuedLogs.Enqueue(new LogMessage 
                { 
                    logType = LogType.Exception, 
                    message = exception.ToString(), 
                    context = context 
                });
            }
            
            public struct LogMessage
            {
                public LogType logType;
                public string message;
                public UnityEngine.Object context;
            }
        }
    }
}