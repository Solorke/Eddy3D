using Rhino.Geometry;
using System.Collections.Generic;

namespace EddyLib.Radiation
{
    public class RSurface
    {
        public string Name;
        public Brep Surface;
        public Mesh LowPoly;
        public Mesh HighPoly;

        public List<RPolygon> Polys;

        public RadiationSurfaceType Type;
        public SimulationType SimulationType;
        public float[] TemperatureOverride;

        public double PatchSize;

        public string MaterialID;
        public RSurface_Settings Settings;
        public VegetationSurface_Settings VegSettings;
        public Tree_Settings TreeSettings;

        public RSurface()
        {
        }

        public void SetMeshes()
        {
        }

        public RSurface(string name, Brep b, RadiationSurfaceType type, SimulationType simtype, Tree_Settings settings, double patchSize = 3)
        {
            Name = name;
            Surface = b;
            PatchSize = patchSize > 0 ? patchSize : 2;
            TreeSettings = settings;

            MaterialID = "";
            RadianceMaterials.GetID(TreeSettings.RadianceMaterial, out MaterialID);

            //Material = refl > 1 ? 1 : refl;
            Type = type;
            SimulationType = simtype;

            // simple mesh for rad sim and obstruction calculation
            MeshingParameters mp_low = new MeshingParameters();
            mp_low.MaximumEdgeLength = 5.0;
            LowPoly = new Mesh();
            foreach (var m in Mesh.CreateFromBrep(b, mp_low))
            {
                LowPoly.Append(m);
            }

            // fine subdivisions for viewfactor analysis
            MeshingParameters mp_high = new MeshingParameters();
            mp_high.MinimumEdgeLength = patchSize;
            mp_high.MaximumEdgeLength = patchSize;

            HighPoly = new Mesh();
            foreach (var m in Mesh.CreateFromBrep(b, mp_high))
            {
                HighPoly.Append(m);
            }

            MakePolys();
        }

        public RSurface(string name, Brep b, RadiationSurfaceType type, SimulationType simtype, VegetationSurface_Settings settings, double patchSize = 3)
        {
            Name = name;
            Surface = b;
            PatchSize = patchSize > 0 ? patchSize : 2;
            VegSettings = settings;

            MaterialID = "";
            RadianceMaterials.GetID(VegSettings.RadianceMaterial, out MaterialID);

            //Material = refl > 1 ? 1 : refl;
            Type = type;
            SimulationType = simtype;

            // simple mesh for rad sim and obstruction calculation
            MeshingParameters mp_low = new MeshingParameters();
            mp_low.MaximumEdgeLength = 5.0;
            LowPoly = new Mesh();
            foreach (var m in Mesh.CreateFromBrep(b, mp_low))
            {
                LowPoly.Append(m);
            }

            // fine subdivisions for viewfactor analysis
            MeshingParameters mp_high = new MeshingParameters();
            mp_high.MinimumEdgeLength = patchSize;
            mp_high.MaximumEdgeLength = patchSize;

            HighPoly = new Mesh();
            foreach (var m in Mesh.CreateFromBrep(b, mp_high))
            {
                HighPoly.Append(m);
            }

            MakePolys();
        }

        public RSurface(string name, Brep b, RadiationSurfaceType type, SimulationType simtype, RSurface_Settings settings, double patchSize = 3)
        {
            Name = name;
            Surface = b;
            PatchSize = patchSize > 0 ? patchSize : 3;
            Settings = settings;

            MaterialID = "";
            RadianceMaterials.GetID(Settings.RadianceMaterial, out MaterialID);

            //Material = refl > 1 ? 1 : refl;
            Type = type;
            SimulationType = simtype;

            // simple mesh for rad sim and obstruction calculation
            //MeshingParameters mp_low = new MeshingParameters();
            //LowPoly = new Mesh();
            //foreach (var m in Mesh.CreateFromBrep(b, mp_low))
            //{
            //    LowPoly.Append(m);
            //}

            MeshingParameters mp_low = new MeshingParameters();
            mp_low.MaximumEdgeLength = 5.0;
            MeshingParameters mp_high = new MeshingParameters();
            mp_high.MaximumEdgeLength = patchSize;
            mp_high.MinimumEdgeLength = patchSize;
            LowPoly = new Mesh();
            HighPoly = new Mesh();

            foreach (var bf in b.Faces)
            {
                Brep f = bf.DuplicateFace(true);
                if (f == null) continue;
                var farea = f.GetArea();
                if (farea < 0.1) continue;

                //LOW Poly
                foreach (var m in Mesh.CreateFromBrep(f, mp_low))
                {
                    LowPoly.Append(m);
                }

                //HIGH Poly
                foreach (var m in Mesh.CreateFromBrep(f, mp_high))
                {
                    HighPoly.Append(m);
                }

                //QuadRemeshParameters qparam = new QuadRemeshParameters();
                //qparam.AdaptiveQuadCount = false;
                //qparam.AdaptiveSize = 0;
                //qparam.DetectHardEdges = true;
                //qparam.TargetQuadCount = (int)(farea / (patchSize * patchSize));
                //var qmesh = Mesh.QuadRemeshBrep(f, qparam);
                //HighPoly.Append(qmesh);
            }

            //// fine subdivisions for viewfactor analysis
            //var area = b.GetArea();
            //QuadRemeshParameters qparam = new QuadRemeshParameters();
            //qparam.AdaptiveQuadCount = false;
            //qparam.AdaptiveSize = 0;
            //qparam.DetectHardEdges = true;
            //qparam.TargetQuadCount = (int)(area / (patchSize * patchSize));
            //HighPoly = Mesh.QuadRemeshBrep(b, qparam);

            MakePolys();
        }

        private void MakePolys(double rad = 0, double refl = 0.5)
        {
            Polys = new List<RPolygon>();
            Mesh _ms = this.HighPoly;
            if (_ms == null) return;

            _ms.FaceNormals.ComputeFaceNormals();

            for (int i = 0; i < _ms.Faces.Count; ++i)
            {
                RPolygon pg = new RPolygon();

                pg.Parent = this;

                Polys.Add(pg);

                if (this.TemperatureOverride != null)
                {
                    pg.TemperatureOverride = this.TemperatureOverride;
                }

                pg.Centroid.Value = _ms.Faces.GetFaceCenter(i);
                pg.Normal.Value = _ms.FaceNormals[i];
                pg.Normal.Value.Unitize();

                pg.rin = rad;
                pg.rout = 0.0;
                pg.refl = refl;

                pg.Name = this.Name + "_" + this.Type.ToString();
                pg.Type = this.Type;
                pg.SimulationType = this.SimulationType;

                if (_ms.Faces[i].IsQuad)
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);
                    Point3d v3 = new Point3d(_ms.Vertices[_ms.Faces[i].D]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);
                    Vector3d n2 = Vector3d.CrossProduct(v2 - v0, v3 - v0);

                    pg.Area = n1.Length * 0.5 + n2.Length * 0.5;

                    pg.Mesh.Value = new Mesh();
                    pg.Mesh.Value.Vertices.Add(v0);
                    pg.Mesh.Value.Vertices.Add(v1);
                    pg.Mesh.Value.Vertices.Add(v2);
                    pg.Mesh.Value.Vertices.Add(v3);
                    pg.Mesh.Value.Faces.AddFace(0, 1, 2, 3);
                }
                else
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);

                    pg.Area = n1.Length * 0.5;

                    pg.Mesh.Value = new Mesh();
                    pg.Mesh.Value.Vertices.Add(v0);
                    pg.Mesh.Value.Vertices.Add(v1);
                    pg.Mesh.Value.Vertices.Add(v2);
                    pg.Mesh.Value.Faces.AddFace(0, 1, 2);
                }
            }
            return;
        }
    }
}