using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Globalization;

namespace _2dot5D_Engine
{
	struct Vector2
	{
		public float x, y;

		public Vector2(float x, float y)
		{
			this.x = x;
			this.y = y;
		}
	}
	struct Vector3
	{
		public float x, y, z;

		public Vector3(float x, float y, float z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}
	class Mesh
	{
		public Vector3[] vertices { get; private set; }
		public Vector3[] normals { get; private set; }
		public Vector2[] uvs { get; private set; }
		public int[] indices { get; private set; }

		public static Mesh ParseFromOBJ(string fileName)
		{
			List<Vector2> uvs = new List<Vector2>();
			List<Vector3> vertices = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<int> indices = new List<int>();

			List<Vector2> finalUVs = new List<Vector2>();
			List<Vector3> finalNormals = new List<Vector3>();

			char[] separator = new char[] { ' ' };
			var numberFormat = CultureInfo.InvariantCulture.NumberFormat;

			using (StreamReader file = new StreamReader(fileName))
			{
				while (!file.EndOfStream)
				{
					string line = file.ReadLine();
					string[] words = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
					float x, y, z;

					switch (words[0])
					{
						case "v":

							x = Convert.ToSingle(words[1], numberFormat);
							y = Convert.ToSingle(words[2], numberFormat);
							z = Convert.ToSingle(words[3], numberFormat);
							vertices.Add(new Vector3(x, y, z));

							break;

						case "vt":

							x = Convert.ToSingle(words[1], numberFormat);
							y = Convert.ToSingle(words[2], numberFormat);
							uvs.Add(new Vector2(x, y));

							break;

						case "vn":

							x = Convert.ToSingle(words[1], numberFormat);
							y = Convert.ToSingle(words[2], numberFormat);
							z = Convert.ToSingle(words[3], numberFormat);
							normals.Add(new Vector3(x, y, z));

							break;

						case "f":
							
							int v0, v1, v2;
							string[] t0 = words[1].Split('/');
							string[] t1 = words[2].Split('/');
							string[] t2 = words[3].Split('/');

							indices.Add(v0 = Convert.ToInt32(t0[0]) - 1);
							indices.Add(v1 = Convert.ToInt32(t1[0]) - 1);
							indices.Add(v2 = Convert.ToInt32(t2[0]) - 1);

							finalUVs.Add(uvs[Convert.ToInt32(t0[1]) - 1]);
							finalUVs.Add(uvs[Convert.ToInt32(t1[1]) - 1]);
							finalUVs.Add(uvs[Convert.ToInt32(t2[1]) - 1]);

							finalNormals.Add(normals[Convert.ToInt32(t0[2]) - 1]);
							finalNormals.Add(normals[Convert.ToInt32(t1[2]) - 1]);
							finalNormals.Add(normals[Convert.ToInt32(t2[2]) - 1]);

							break;
					}
				}
			}
			Mesh mesh = new Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.uvs = finalUVs.ToArray();
			mesh.normals = finalNormals.ToArray();
			mesh.indices = indices.ToArray();
			return mesh;
		}
	}
    static class Program
    {
		static StreamWriter log;

		[STAThread]
        static void Main()
        {
			log = new StreamWriter("./Data/log.txt");
			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new MainForm());
			}
			catch (Exception ex)
			{
				LogError(ex.ToString());
			}
			finally
			{
				log.Close();
			}
        }

		public static void Log(string message)
		{
			log.WriteLine(message);
		}
		public static void LogError(string message)
		{
			MessageBox.Show(message);
			throw new Exception("Error");
		}
	}
    class MainForm : Form
    {
		Mesh mesh;
		Bitmap texture;

		public MainForm()
		{
			mesh = Mesh.ParseFromOBJ("./Data/Soldier.obj");
			texture = new Bitmap("./Data/Soldier.png");
			SizeChanged += (s, e) => Refresh();
		}

		static PointF[] pointsNonAlloc = new PointF[3];

		struct Triangle
		{
			public PointF v0, v1, v2;
			public Vector2 uv0, uv1, uv2;
			public float queue;
			public Color color;

			public Triangle(PointF v0, PointF v1, PointF v2, float queue, Color color, Vector2 uv0, Vector2 uv1, Vector2 uv2)
			{
				this.v0 = v0;
				this.v1 = v1;
				this.v2 = v2;
				this.queue = queue;
				this.color = color;
				this.uv0 = uv0;
				this.uv1 = uv1;
				this.uv2 = uv2;
			}
		}

		float Loop(float f)
		{
			f = f - (int)f;
			if (f < 0) f = 1f + f;
			return f;
		}

		protected override void OnPaint(PaintEventArgs e)
        {
			List<Triangle> triangles = new List<Triangle>();
			IEnumerable<Triangle> orderedTriangles = triangles.OrderBy(t => t.queue);

			float offsetX = Width * 0.5f;
			float offsetY = Height * 0.75f;
			float modelSize = Height * 0.1f;
			e.Graphics.Clear(Color.Black);

			for (int i = 0; i < mesh.indices.Length; i+=3)
			{
				Vector3 v0, v1, v2;
				try
				{
					int i0, i1, i2;
					v0 = mesh.vertices[i0 = mesh.indices[i]];
					v1 = mesh.vertices[i1 = mesh.indices[i + 1]];
					v2 = mesh.vertices[i2 = mesh.indices[i + 2]];
					v0.y = -v0.y;
					v1.y = -v1.y;
					v2.y = -v2.y;

					pointsNonAlloc[0].X = v0.x * modelSize + offsetX;
					pointsNonAlloc[0].Y = v0.y * modelSize + offsetY;

					pointsNonAlloc[1].X = v1.x * modelSize + offsetX;
					pointsNonAlloc[1].Y = v1.y * modelSize + offsetY;

					pointsNonAlloc[2].X = v2.x * modelSize + offsetX;
					pointsNonAlloc[2].Y = v2.y * modelSize + offsetY;

					float brightness = (1f + (v0.z + v1.z + v2.z) * 0.3333f) * 0.5f;

					Color clr = Color.White;// texture.GetPixel((int)(mesh.uvs[i0].x * texture.Width), (int)(mesh.uvs[i0].y * texture.Height));
					var color = Color.FromArgb((int)(clr.R * brightness), (int)(clr.G * brightness), (int)(clr.B * brightness));

					triangles.Add(new Triangle(
						pointsNonAlloc[0],
						pointsNonAlloc[1],
						pointsNonAlloc[2],
						(v0.z + v1.z + v2.z) * 0.3333f,
						color,
						mesh.uvs[i0],
						mesh.uvs[i1],
						mesh.uvs[i2]));
				}
				catch (Exception ex)
				{
					Program.LogError(ex.ToString());
				}
			}
			foreach (var triangle in orderedTriangles)
			{
				pointsNonAlloc[0].X = triangle.uv0.x;
				pointsNonAlloc[0].Y = triangle.uv0.y;

				pointsNonAlloc[1].X = triangle.uv1.x;
				pointsNonAlloc[1].Y = triangle.uv1.y;

				pointsNonAlloc[2].X = triangle.uv2.x;
				pointsNonAlloc[2].Y = triangle.uv2.y;

				pointsNonAlloc[0].X = triangle.v0.X;
				pointsNonAlloc[0].Y = triangle.v0.Y;

				pointsNonAlloc[1].X = triangle.v1.X;
				pointsNonAlloc[1].Y = triangle.v1.Y;

				pointsNonAlloc[2].X = triangle.v2.X;
				pointsNonAlloc[2].Y = triangle.v2.Y;

				e.Graphics.FillPolygon(new SolidBrush(triangle.color), pointsNonAlloc);
			}
		}
    }
}
