﻿using System;
namespace Toggl.Ultrawave.Network
{
    internal struct LocationEndpoints
    {
        private readonly Uri baseUrl;

        public LocationEndpoints(Uri baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public Endpoint Get => Endpoint.Get(baseUrl, "me/location");
    }
}
