﻿using System;

namespace Ductus.FluentDocker.Services
{
  public sealed class ServiceDelegates
  {
    public delegate void StateChange(object sender, StateChangeEventArgs evt);
  }

  public interface IService : IDisposable
  {
    string Name { get; }
    ServiceRunningState State { get; }
    void Start();
    void Stop();
    void Remove(bool force = false);

    event ServiceDelegates.StateChange StateChange;
  }
}