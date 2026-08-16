using System;
using System.Diagnostics;

namespace Opure.Runtime.Contracts.Models;

public readonly struct ModelHostSession : IEquatable<ModelHostSession>
{
    public Guid SessionId { get; }
    public IntPtr JobObjectHandle { get; }
    public Process Process { get; }
    public DateTime LaunchTime { get; }

    public bool IsRunning
    {
        get
        {
            if (Process == null) return false;
            Process.Refresh();
            return !Process.HasExited;
        }
    }

    public ModelHostSession(Guid sessionId, IntPtr jobObjectHandle, Process process, DateTime launchTime)
    {
        SessionId = sessionId;
        JobObjectHandle = jobObjectHandle;
        Process = process;
        LaunchTime = launchTime;
    }

    public bool Equals(ModelHostSession other) => SessionId == other.SessionId;
    public override bool Equals(object? obj) => obj is ModelHostSession other && Equals(other);
    public override int GetHashCode() => SessionId.GetHashCode();
    public static bool operator ==(ModelHostSession left, ModelHostSession right) => left.Equals(right);
    public static bool operator !=(ModelHostSession left, ModelHostSession right) => !(left == right);
    public override string ToString() => $"ModelHostSession[Id={SessionId:D}, Running={IsRunning}]";
}
