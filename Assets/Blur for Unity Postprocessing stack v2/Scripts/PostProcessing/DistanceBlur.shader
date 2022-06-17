// MIT License
//
// Copyright (c) 2018 Oxeren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// Downloaded from:
// https://github.com/Oxeren/Blur-Distance-Blur-for-Unity-Postprocessing-Stack-v2

Shader "Hidden/Custom/DistanceBlur"
{
	HLSLINCLUDE

	#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
	#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Sampling.hlsl"

	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
	TEXTURE2D_SAMPLER2D(_DryTex, sampler_DryTex);
	float _StartDistance;
	float _EndDistance;
	float4 _MainTex_TexelSize;

	ENDHLSL

	SubShader 
	{
		Cull Off ZWrite Off ZTest Always

		Pass //0 Applying Box filtering
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment Frag

			float4 Frag(VaryingsDefault i) : SV_Target
			{
				return DownsampleBox13Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord,  _MainTex_TexelSize.xy);
			}

			ENDHLSL 
		}

		Pass // 1 Mixing dry input with blurred texture by depth
		{
			
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment Frag

			float4 Frag(VaryingsDefault i) : SV_Target
			{
				float4 blurredColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
				float4 dryColor = SAMPLE_TEXTURE2D(_DryTex, sampler_DryTex, i.texcoord);
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord);
				depth = Linear01Depth(depth);
				return lerp(dryColor, blurredColor, saturate((depth - _StartDistance) / (_EndDistance - _StartDistance)) );
			} 

			ENDHLSL 
		
		}
	}

}