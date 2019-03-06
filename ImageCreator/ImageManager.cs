using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using QuickFont;
using QuickFont.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageCreator
{
    static class ImageManager
    {
        const int TEXT_BORDER_SPACE = 20;
        const String FONT_LOCATION = "data/arial.ttf";
        const String VERTEX_SHADER_LOCATION = "data/simplevs.glsl";
        const String FRAGMENT_SHADER_LOCATION = "data/simplefs.glsl";
        const int COLOR_CALCULATOR_SAMPLE_FREQ = 40;

        public static ImageData ChooseImage(List<ImageData> images)
        {
#if LOG_DATA
            DataLogger.Log("[ImageManager] Choosing image from group of " + images.Count, LoggingMode.Message);
#endif
            int currentHighScore = Int32.MinValue;
            int highestScoreIndex = -1;
            int score;

            for (int i = 0; i < images.Count; i++)
            {
                ImageData img = images[i];
                score = (int)Math.Sqrt(Math.Min(img.Height, 1200) * Math.Min(img.Width, 1200)) / 5;
                float ar = (float)img.Width / (float)img.Height; //aspect ratio between width and height
                score += (int)(-2 * ar * ar * ar + 3 * ar * ar) * 250; //a simple function to encourage nice width-height ratios
                if (img.DisplayLink.Contains("wikipedia"))
                    score += 105;
                else if (img.DisplayLink.EndsWith(".org"))
                    score += 75;

                if (score > currentHighScore)
                {
                    currentHighScore = score;
                    highestScoreIndex = i;
                }
            }
#if LOG_DATA
            DataLogger.Log(String.Concat("[ImageManager] Chose image ", highestScoreIndex, " with score ", currentHighScore), LoggingMode.Message);
#endif
            return images[highestScoreIndex];
        }

        public static void CreateImage(String singular, String plural, String imagePath, String resultPath)
        {
#if LOG_DATA
            DataLogger.Log(String.Concat("[ImageManager] Creating image with: singular=\"", singular, "\" plural=\"", plural, "\" imagePath=\"", imagePath, "\" resultPath=\"", resultPath, "\""), LoggingMode.Message);
            DataLogger.Log("[ImageManager] Creating OpenGL Context...", LoggingMode.Message);
#endif
            GraphicsMode g = new GraphicsMode(ColorFormat.Empty, 0, 0, 0, ColorFormat.Empty, 1, false);
            GameWindow window = new GameWindow(1, 1, g, "ThingsBeLike_ImageCreator", GameWindowFlags.Default, DisplayDevice.Default, 5, 4, GraphicsContextFlags.Offscreen);

#if LOG_DATA
            DataLogger.Log("[ImageManager] Printing OpenGL Context data: ", LoggingMode.Message);
            DataLogger.Log(String.Concat("GL_MXXOR_VERSION [Minor, Major]: [" + GL.GetInteger(GetPName.MinorVersion), ", ", GL.GetInteger(GetPName.MajorVersion), "]"), LoggingMode.RawData);
            DataLogger.Log("GL_VERSION: " + GL.GetString(StringName.Version), LoggingMode.RawData);
            DataLogger.Log("GL_VENDOR: " + GL.GetString(StringName.Vendor), LoggingMode.RawData);
            DataLogger.Log("GL_RENDERER" + GL.GetString(StringName.Renderer), LoggingMode.RawData);
            DataLogger.Log("GL_SHADING_LANGUAGE_VERSION: " + GL.GetString(StringName.ShadingLanguageVersion), LoggingMode.RawData);
            DataLogger.Log("[ImageManager] [END OF OPENGL CONTEXT DATA MESSAGE]", LoggingMode.Message);
#endif

            int tex, fbo, width, height;
            tex = MakeAllImage(singular, plural, imagePath, out fbo, out width, out height);
            Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

#if LOG_DATA
            DataLogger.Log("[ImageManager] Reading pixels from framebuffer", LoggingMode.Message);
#endif
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bits.Scan0);

            bitmap.UnlockBits(bits);
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

#if LOG_DATA
            DataLogger.Log("[ImageManager] Deleting last OpenGL resources (returned Framebuffer & Texture)", LoggingMode.Message);
#endif
            GL.DeleteFramebuffer(fbo);
            GL.DeleteTexture(tex);
#if LOG_DATA
            DataLogger.Log(String.Concat("[ImageManager] Saving image as \"", resultPath, "\""), LoggingMode.Message);
#endif
            bitmap.Save(resultPath, ImageFormat.Jpeg);
#if LOG_DATA
            DataLogger.Log(String.Concat("[ImageManager] Saved image as \"", resultPath, "\""), LoggingMode.Success);
            DataLogger.Log("[ImageManager] Disposing bitmap and window. Closing OpenGL Context", LoggingMode.Message);
#endif

            bitmap.Dispose();
            window.Dispose();
#if LOG_DATA
            DataLogger.Log(String.Concat("[ImageManager] Done creating image. Saved at: \"", resultPath, "\""), LoggingMode.Success);
#endif
        }

        /// <summary>
        /// Creates the image on an OpenGL 2D texture and returns it's OpenGL handle. 
        /// This function creates and destroys all the OpenGL objects it uses except the texture and framebuffer.
        /// </summary>
        /// <param name="singular">The singular form of the word</param>
        /// <param name="plural">The plural form of the word</param>
        /// <param name="imagePath">The path to the image showing the word to use</param>
        /// <param name="fbo">The framebuffer object that was used to render to the returned texture, still with the texture attached in color0</param>
        /// <param name="WIDTH">The width of the texture</param>
        /// <param name="HEIGHT">The height of the texture</param>
        static int MakeAllImage(String singular, String plural, String imagePath, out int fbo, out int WIDTH, out int HEIGHT)
        {
#if LOG_DATA
            DataLogger.Log("[ImageManager] MakeAllImage was called", LoggingMode.Message);
#endif

            WIDTH = 800;
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusDstAlpha);

#if LOG_DATA
            DataLogger.Log(String.Concat("[ImageManager] Loading bitmap from \"", imagePath, "\""), LoggingMode.Message);
#endif
            Bitmap bitmap = new Bitmap(imagePath);

            #region CalculateTextColor
#if LOG_DATA
            DataLogger.Log("[ImageManager] Calculating text color", LoggingMode.Message);
#endif
            //calculate whether to make the *noise text* black or white based on the colors on the image.
            float diffX = bitmap.Width / (float)COLOR_CALCULATOR_SAMPLE_FREQ;
            float diffY = bitmap.Height / (float)COLOR_CALCULATOR_SAMPLE_FREQ;

            int grayscaleTotal = 0;
            for (float x = (bitmap.Width - bitmap.Width / COLOR_CALCULATOR_SAMPLE_FREQ * COLOR_CALCULATOR_SAMPLE_FREQ) / 2f; x < bitmap.Width; x += diffX)
                for (float y = (bitmap.Height - bitmap.Height / COLOR_CALCULATOR_SAMPLE_FREQ * COLOR_CALCULATOR_SAMPLE_FREQ) / 2f; y < bitmap.Height; y += diffY)
                {
                    Color c = bitmap.GetPixel((int)x, (int)y);
                    grayscaleTotal += c.R;
                    grayscaleTotal += c.G;
                    grayscaleTotal += c.B;
                }
            bool isBlack = grayscaleTotal / (COLOR_CALCULATOR_SAMPLE_FREQ * COLOR_CALCULATOR_SAMPLE_FREQ * 3) > 127;
#if LOG_DATA
            DataLogger.Log("[ImageManager] Text color: " + (isBlack ? "black" : "white"), LoggingMode.Message);
#endif
            #endregion

            #region LoadQFont
#if LOG_DATA
            DataLogger.Log("[ImageManager] Loading QFont data", LoggingMode.Message);
#endif
            QFontShadowConfiguration shadowConfig = new QFontShadowConfiguration()
            {
                Type = ShadowType.Expanded,
                BlurRadius = 2,
            };
            QFontBuilderConfiguration qconfig = new QFontBuilderConfiguration(true)
            {
                SuperSampleLevels = 4,
                TextGenerationRenderHint = TextGenerationRenderHint.AntiAlias,
            };
            QFont qfont = new QFont(new FreeTypeFont(FONT_LOCATION, 144, FontStyle.Regular), qconfig);
            QFontDrawing qdraw = new QFontDrawing();
            QFontRenderOptions opts = new QFontRenderOptions()
            {
                CharacterSpacing = 0.06f,
                Colour = Color.Black,
            };
            ProcessedText text = QFontDrawingPrimitive.ProcessText(qfont, opts, plural + " be like", new SizeF(WIDTH - TEXT_BORDER_SPACE * 2, 99999f), QFontAlignment.Left);
            SizeF topTextSize = qdraw.Print(qfont, text, new Vector3(0, 0, 0), opts);

            qdraw.RefreshBuffers();

            #endregion

            int topHeight = 40 + (int)(topTextSize.Height + 0.5f);
            HEIGHT = WIDTH * bitmap.Height / bitmap.Width + topHeight;

#if LOG_DATA
            DataLogger.Log(String.Concat("[ImageManager] WIDTH=", WIDTH, " HEIGHT=", HEIGHT, " topHeight=", topHeight), LoggingMode.Message);
#endif

            #region GenVBO
#if LOG_DATA
            DataLogger.Log("[ImageManager] Generating VBO and VAO", LoggingMode.Message);
#endif
            float[] vboData = new float[]
            {
                0, 0, 0, 0, 1,
                0, 1, 0, 0, 0,
                1, 0, 0, 1, 1,
                1, 1, 0, 1, 0
            };
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vboData.Length * 4, vboData, BufferUsageHint.StaticDraw);
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 20, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 20, 12);
            GL.EnableVertexAttribArray(1);
            #endregion

            #region LoadTexture
#if LOG_DATA
            DataLogger.Log("[ImageManager] Generating texture for image", LoggingMode.Message);
#endif
            int tex = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex);
#if LOG_DATA
            DataLogger.Log("[ImageManager] Loading texture pixels from bitmap", LoggingMode.Message);
#endif
            BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bits.Scan0);
            bitmap.UnlockBits(bits);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            #endregion

            #region LoadShaderProgram
#if LOG_DATA
            DataLogger.Log("[ImageManager] Creating shaders", LoggingMode.Message);
#endif
            int vs = GL.CreateShader(ShaderType.VertexShader);
#if LOG_DATA
            DataLogger.Log("[ImageManager] Loading vertex shader from " + VERTEX_SHADER_LOCATION, LoggingMode.Message);
#endif
            GL.ShaderSource(vs, File.ReadAllText(VERTEX_SHADER_LOCATION));
            GL.CompileShader(vs);
            int tmp;
            GL.GetShader(vs, ShaderParameter.CompileStatus, out tmp);
#if LOG_DATA
            if (tmp != 1)
                DataLogger.Log("[ImageManager] Vertex shader compilation failed", LoggingMode.Error);
            DataLogger.Log("[ImageManager] Vertex Shader Info Log: ", LoggingMode.Message);
            DataLogger.Log(GL.GetShaderInfoLog(vs), LoggingMode.RawData);
            DataLogger.Log("[ImageManager] [END OF VERTEX SHADER INFO LOG]", LoggingMode.Message);
#endif
            if (tmp != 1)
                throw new Exception("Vertex Shader compilation failed. Process can't continue.");

            int fs = GL.CreateShader(ShaderType.FragmentShader);
#if LOG_DATA
            DataLogger.Log("[ImageManager] Loading fragment shader from " + FRAGMENT_SHADER_LOCATION, LoggingMode.Message);
#endif
            GL.ShaderSource(fs, File.ReadAllText(FRAGMENT_SHADER_LOCATION));
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out tmp);
#if LOG_DATA
            if (tmp != 1)
                DataLogger.Log("[ImageManager] Fragment shader compilation failed", LoggingMode.Error);
            DataLogger.Log("[ImageManager] Fragment Shader Info Log: ", LoggingMode.Message);
            DataLogger.Log(GL.GetShaderInfoLog(fs), LoggingMode.RawData);
            DataLogger.Log("[ImageManager] [END OF FRAGMENT SHADER INFO LOG]", LoggingMode.Message);
#endif
            if (tmp != 1)
                throw new Exception("Fragment Shader compilation failed. Process can't continue.");

#if LOG_DATA
            DataLogger.Log("[ImageManager] Performing OpenGL program creation commands", LoggingMode.Message);
#endif
            int program = GL.CreateProgram();
            GL.AttachShader(program, vs);
            GL.AttachShader(program, fs);
            GL.BindAttribLocation(program, 0, "vPosition");
            GL.BindAttribLocation(program, 1, "vTexCoords");
            GL.LinkProgram(program);
            GL.DetachShader(program, vs);
            GL.DetachShader(program, fs);
            GL.DeleteProgram(vs);
            GL.DeleteProgram(fs);
            int texUniformLoc = GL.GetUniformLocation(program, "tex");
            int projUniformLoc = GL.GetUniformLocation(program, "Proj");
            GL.UseProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out tmp);

#if LOG_DATA
            if (tmp != 1)
                DataLogger.Log("[ImageManager] GL Program linking failed", LoggingMode.Error);
            DataLogger.Log("[ImageManager] GL Program Info Log: ", LoggingMode.Message);
            DataLogger.Log(GL.GetProgramInfoLog(program), LoggingMode.RawData);
            DataLogger.Log("[ImageManager] [END OF GL PROGRAM INFO LOG]", LoggingMode.Message);
            DataLogger.Log("[ImageManager] Just a friendly reminder that GL PROGRAM refers to a OpenGL Shader Program with attached shaders", LoggingMode.Message);
#endif
            if (tmp != 1)
                throw new Exception("Program linking failed. Process can't continue.");
            #endregion

            #region MakeResultTextureFramebuffer
#if LOG_DATA
            DataLogger.Log("[ImageManager] Generating Framebuffer & Texture for rendering", LoggingMode.Message);
#endif
            int resultTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, resultTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, WIDTH, HEIGHT, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, resultTex, 0);
            if(GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
            { //error
#if LOG_DATA
                DataLogger.Log("[ImageManager] Framebuffer creation failed", LoggingMode.Error);
#endif
                throw new Exception("Framebuffer creation failed. Process can't continue.");
            }

            #endregion

            #region Drawing
#if LOG_DATA
            DataLogger.Log("[ImageManager] Performing OpenGL draw commands", LoggingMode.Message);
#endif
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            GL.ClearColor(1f, 1f, 1f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(program);
            Matrix4 mat = Matrix4.CreateOrthographicOffCenter(0, 1, 0, 1, -1, 1);
            GL.UniformMatrix4(projUniformLoc, false, ref mat);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex);
            GL.Uniform1(texUniformLoc, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vao);
            GL.Viewport(0, 0, WIDTH, HEIGHT - topHeight);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.Viewport(0, HEIGHT - topHeight, WIDTH, topHeight);
            qdraw.ProjectionMatrix = Matrix4.CreateTranslation(TEXT_BORDER_SPACE, topTextSize.Height + TEXT_BORDER_SPACE, 0) * Matrix4.CreateOrthographicOffCenter(0, WIDTH, 0, topHeight, -1, 1);
            qdraw.Draw();

            GL.Viewport(0, 0, WIDTH, HEIGHT - topHeight);

            opts.DropShadowActive = true;
            opts.DropShadowColour = Color.White;
            opts.DropShadowOffset = Vector2.Zero;//new Vector2(-0.12f, 0.12f);
            text = QFontDrawingPrimitive.ProcessText(qfont, opts, String.Concat("*", singular, " noises*"), new SizeF(WIDTH - TEXT_BORDER_SPACE * 2, 99999f), QFontAlignment.Centre);
            SizeF noiseTextSize = qdraw.Print(qfont, text, new Vector3(0, 0, 0), opts);
            float textScale = Math.Min((WIDTH - TEXT_BORDER_SPACE * 4) / noiseTextSize.Width, (HEIGHT - TEXT_BORDER_SPACE * 4) / noiseTextSize.Height);

#if LOG_DATA
            DataLogger.Log("[ImageManager] Updating QFont size (disposing and reloading)", LoggingMode.Message);
#endif
            qfont.Dispose();
            qconfig.ShadowConfig = shadowConfig;
            qfont = new QFont(new FreeTypeFont(FONT_LOCATION, 144 * textScale, FontStyle.Regular), qconfig);

            qdraw.DrawingPrimitives.Clear();
            opts.Colour = isBlack ? Color.Black : Color.White;
            opts.DropShadowColour = isBlack ? Color.White : Color.Black;
            text = QFontDrawingPrimitive.ProcessText(qfont, opts, String.Concat("*", singular, " noises*"), new SizeF(WIDTH - TEXT_BORDER_SPACE * 2, 99999f), QFontAlignment.Centre);
            qdraw.Print(qfont, text, new Vector3(0, 0, 0), opts);
            qdraw.ProjectionMatrix = Matrix4.CreateTranslation(WIDTH / 2f, noiseTextSize.Height * textScale / 2f + (HEIGHT - topHeight) / 2f, 0f) * Matrix4.CreateOrthographicOffCenter(0, WIDTH, 0, HEIGHT - topHeight, -1, 1);
            qdraw.RefreshBuffers();
            qdraw.Draw();
#if LOG_DATA
            DataLogger.Log("[ImageManager] Done drawing", LoggingMode.Success);
#endif
            #endregion

            #region Disposing
#if LOG_DATA
            DataLogger.Log("[ImageManager] Disposing MakeAllImage resources", LoggingMode.Message);
#endif
            bitmap.Dispose();
            qfont.Dispose();
            qdraw.Dispose();

            GL.DeleteProgram(program);
            GL.DeleteTexture(tex);
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
            #endregion

#if LOG_DATA
            DataLogger.Log("[ImageManager] MakeAllImage is done. Returning texture and framebuffer data", LoggingMode.Message);
#endif
            return resultTex;
        }
    }
}
