﻿using Ductus.FluentDocker.Model;
using Ductus.FluentDocker.Services;

namespace Ductus.FluentDocker.Extensions
{
  public static class ModelExtensions
  {
    public static ServiceRunningState ToServiceState(this ContainerState state)
    {
      if (null == state)
      {
        return ServiceRunningState.Unknown;
      }

      if (state.Running)
      {
        return ServiceRunningState.Running;
      }

      if (state.Dead)
      {
        return ServiceRunningState.Stopped;
      }

      if (state.Restarting)
      {
        return ServiceRunningState.Starting;
      }

      if (state.Paused)
      {
        return ServiceRunningState.Paused;
      }

      return ServiceRunningState.Unknown;
    }
  }
}