﻿using System;
using Ductus.FluentDocker.Services;

namespace Ductus.FluentDocker.Model
{
  public sealed class MachineLsResponse
  {
    public string Name { get; set; }
    public ServiceRunningState State { get; set; }
    public Uri Docker { get; set; }
  }
}