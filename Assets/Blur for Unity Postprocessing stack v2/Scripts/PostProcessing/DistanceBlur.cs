/*
MIT License

Copyright (c) 2018 Oxeren

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

Downloaded from:
https://github.com/Oxeren/Blur-Distance-Blur-for-Unity-Postprocessing-Stack-v2
*/

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable, PostProcess(typeof(DistanceBlurRenderer), PostProcessEvent.AfterStack, "Hidden/Custom/DistanceBlur")]
public sealed class DistanceBlur : PostProcessEffectSettings {

	[Range(1, 16)]
	public IntParameter Iterations = new IntParameter { value = 5 };
	public BoolParameter EnableDistance = new BoolParameter { value = true };
	[Range(0f, 1f)]
	public FloatParameter StartDistance = new FloatParameter { value = 10f };
	[Range(0f, 1f)]
	public FloatParameter EndDistance = new FloatParameter { value = 20f };

	public override bool IsEnabledAndSupported(PostProcessRenderContext context) {
		return enabled;
	}
}

public sealed class DistanceBlurRenderer : PostProcessEffectRenderer<DistanceBlur> {

	RenderTexture[] textures = new RenderTexture[16];
	PropertySheet sheet;

	public override void Render(PostProcessRenderContext context) {
		if(sheet == null)
			sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/DistanceBlur"));
		sheet.properties.SetFloat("_StartDistance", settings.StartDistance);
		sheet.properties.SetFloat("_EndDistance", settings.EndDistance);
		int width = context.width;
		int height = context.height;
		var dry = RenderTexture.GetTemporary(context.width, context.height, 0, context.sourceFormat);
		var currentDestination = textures[0] = RenderTexture.GetTemporary(context.width, context.height, 0, context.sourceFormat);
		context.command.BlitFullscreenTriangle(context.source, currentDestination, sheet, 0);
		var currentSource = currentDestination;
		// Donwsampling
		int i = 1;
		for (; i < settings.Iterations; i++) {
			width /= 2;
			height /= 2;
			if (height < 2 || width < 2) {
				Debug.LogWarning("Too high iteration count for the screen size, dropping remaining iterations");
				break;
			}
			currentDestination = textures[i] = RenderTexture.GetTemporary(width, height, 0, context.sourceFormat);
			context.command.BlitFullscreenTriangle(currentSource, currentDestination, sheet, 0);
			textures[i] = currentDestination;
			currentSource = currentDestination;
		}
		// Upsampling
		for(i -= 2; i >= 0; i--) {
			currentDestination = textures[i];
			context.command.BlitFullscreenTriangle(currentSource, currentDestination, sheet, 0);
			currentSource = currentDestination;
		}
		sheet.properties.SetTexture("_DryTex", dry);
		// Rendering to screen
		if (settings.EnableDistance)
			context.command.BlitFullscreenTriangle(currentSource, context.destination, sheet, 1);
		else
			context.command.BlitFullscreenTriangle(currentSource, context.destination);
		// Cleanup
		RenderTexture.ReleaseTemporary(dry);
		for (i = 0; i < textures.Length; i++)
			if (textures[i] != null) {
				RenderTexture.ReleaseTemporary(textures[i]);
				textures[i] = null;
			}
	}

}