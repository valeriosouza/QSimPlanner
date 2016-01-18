﻿using System.Xml.Linq;

namespace QSP.RouteFinding.Tracks.Common
{
    public abstract class TrackMessage<T> : IXmlConvertible where T : ITrack
    {
        public abstract void LoadFromXml(XDocument doc);
        public abstract override string ToString();
        public abstract XDocument ToXml();
    }
}