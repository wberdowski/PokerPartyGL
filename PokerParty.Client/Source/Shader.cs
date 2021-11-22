using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PokerParty.Client
{
    public class Shader : IDisposable
    {
        public int Handle { get; }

        private int _vertexShaderHandle;
        private int _fragmentShaderHandle;
        private bool _disposed;
        Dictionary<string, int> uniformLocCache = new Dictionary<string, int>();

        public Shader(string vertexPath, string fragmentPath)
        {
            // Load

            string vertexShaderSource;

            using (var reader = new StreamReader(vertexPath, Encoding.UTF8))
            {
                vertexShaderSource = reader.ReadToEnd();
            }

            string fragmentShaderSource;

            using (var reader = new StreamReader(fragmentPath, Encoding.UTF8))
            {
                fragmentShaderSource = reader.ReadToEnd();
            }

            // Bind source

            _vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(_vertexShaderHandle, vertexShaderSource);

            _fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(_fragmentShaderHandle, fragmentShaderSource);

            // Compile

            GL.CompileShader(_vertexShaderHandle);

            string infoLogVert = GL.GetShaderInfoLog(_vertexShaderHandle);
            if (infoLogVert != string.Empty)
            {
                Debug.WriteLine(infoLogVert);
            }

            GL.CompileShader(_fragmentShaderHandle);

            string infoLogFrag = GL.GetShaderInfoLog(_fragmentShaderHandle);

            if (infoLogFrag != string.Empty)
            {
                Debug.WriteLine(infoLogFrag);
            }

            // Create program

            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, _vertexShaderHandle);
            GL.AttachShader(Handle, _fragmentShaderHandle);

            GL.LinkProgram(Handle);

            // Cleanup

            GL.DetachShader(Handle, _vertexShaderHandle);
            GL.DetachShader(Handle, _fragmentShaderHandle);
            GL.DeleteShader(_fragmentShaderHandle);
            GL.DeleteShader(_vertexShaderHandle);
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        public int GetUniformLocation(string attribName)
        {
            if (uniformLocCache.TryGetValue(attribName, out int val))
            {
                return val;
            }
            else
            {
                var v = GL.GetUniformLocation(Handle, attribName);
                uniformLocCache.Add(attribName, v);
                return v;
            }
        }

        public void SetMatrix4(string attribName, Matrix4 matrix)
        {
            GL.UniformMatrix4(GetUniformLocation(attribName), false, ref matrix);
        }

        public void SetVec3(string attribName, Vector3 vector)
        {
            GL.Uniform3(GetUniformLocation(attribName), vector);
        }

        public void SetInt(string attribName, int value)
        {
            GL.Uniform1(GetUniformLocation(attribName), value);
        }

        public void SetFloat(string attribName, float value)
        {
            GL.Uniform1(GetUniformLocation(attribName), value);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                GL.DeleteProgram(Handle);

                _disposed = true;
            }
        }

        ~Shader()
        {
            GL.DeleteProgram(Handle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
