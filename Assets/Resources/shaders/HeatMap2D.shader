﻿/******************************************************************************************************************************************************
* MIT License																																		  *
*																																					  *
* Copyright (c) 2020																																  *
* Emmanuel Badier <emmanuel.badier@gmail.com>																										  *
* 																																					  *
* Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),  *
* to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,  *
* and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:		  *
* 																																					  *
* The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.					  *
* 																																					  *
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, *
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 																							  *
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 		  *
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.							  *
******************************************************************************************************************************************************/

Shader "HeatMap2D"
{
	//Properties
	//{
	//	[NoScaleOffset] _HeatTex("HeatTexture", 2D) = "white" {}
	//}

	//SubShader
	//{
	//	Tags
 //       {
 //           "Queue"="Transparent"
 //           "IgnoreProjector"="True"
 //           "RenderType"="Transparent"
 //           "PreviewType"="Plane"
 //           "CanUseSpriteAtlas"="True"
 //       }

	//	Stencil
 //       {
 //           Ref [_Stencil]
 //           Comp [_StencilComp]
 //           Pass [_StencilOp]
 //           ReadMask [_StencilReadMask]
 //           WriteMask [_StencilWriteMask]
 //       }

 //       Cull Off
 //       Lighting Off
 //       ZWrite Off
 //       ZTest [unity_GUIZTestMode]
 //       Blend SrcAlpha OneMinusSrcAlpha
 //       ColorMask [_ColorMask]

	//	Pass
	//	{
	//		CGPROGRAM
	//		#pragma vertex vert
 //           #pragma fragment frag
 //           #pragma target 2.0

 //           #include "UnityCG.cginc"
 //           #include "UnityUI.cginc"

 //           #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
 //           #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

	//		uniform float4 _Points[1023];
	//		uniform float _InvRadius = 10.0f;
	//		uniform float _Intensity = 0.1f;
	//		uniform int _Count = 0;

	//		sampler2D _HeatTex;
	//		sampler2D _MainTex;

 //           struct appdata_t
 //           {
 //               float4 vertex   : POSITION;
 //               float4 color    : COLOR;
 //               float2 texcoord : TEXCOORD0;
 //               UNITY_VERTEX_INPUT_INSTANCE_ID
 //           };

 //           struct v2f
 //           {
 //               float4 vertex   : SV_POSITION;
 //               fixed4 color    : COLOR;
 //               float2 texcoord  : TEXCOORD0;
 //               float4 worldPosition : TEXCOORD1;
 //               UNITY_VERTEX_OUTPUT_STEREO
 //           };


 //           fixed4 _Color;
 //           fixed4 _TextureSampleAdd;
 //           float4 _ClipRect;
 //           float4 _MainTex_ST;

 //           v2f vert(appdata_t v)
 //           {
 //               v2f OUT;
 //               UNITY_SETUP_INSTANCE_ID(v);
 //               UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
 //               OUT.worldPosition = v.vertex;
 //               OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

 //               OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

 //               OUT.color = v.color * _Color;
 //               return OUT;
 //           }

	//		/*
 //           fixed4 frag(v2f IN) : SV_Target
 //           {
 //               half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

 //               #ifdef UNITY_UI_CLIP_RECT
 //               color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
 //               #endif

 //               #ifdef UNITY_UI_ALPHACLIP
 //               clip (color.a - 0.001);
 //               #endif

 //               return color;
 //           }
	//		*/

	//		float4 frag(v2f input) : SV_TARGET
	//		{
	//			float heat = 0.0f, dist;
	//			float2 vec;
	//			for (int i = 0; i < _Count; ++i)
	//			{
	//				vec = input.texcoord - _Points[i].xy;
	//				dist = sqrt(vec.x * vec.x + vec.y * vec.y);
	//				heat += (1.0f - saturate(dist * _InvRadius)) * _Intensity * _Points[i].z;
	//			}
	//			float4 outc = tex2D(_HeatTex, float2(saturate(heat), 0.5f));
	//			outc.w = 1;
	//			return float4(1.0f, 0.0f, 0.0f, 1.0f);
	//			//return outc;
	//		}

	//		ENDCG
	//	}
	//}
Properties
{
    [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
    _Color("Tint", Color) = (1,1,1,1)

    _StencilComp("Stencil Comparison", Float) = 8
    _Stencil("Stencil ID", Float) = 0
    _StencilOp("Stencil Operation", Float) = 0
    _StencilWriteMask("Stencil Write Mask", Float) = 255
    _StencilReadMask("Stencil Read Mask", Float) = 255

    _ColorMask("Color Mask", Float) = 15

    [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0

    [NoScaleOffset] _HeatTex("HeatTexture", 2D) = "white" {}
}

SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _HeatTex;
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            uniform float4 _Points[1023];
    		uniform float _Intensity = 100.0f;
            uniform float _Radius = 1.0f;
            uniform float _ImgWidth = 1.0f;
            uniform float _ImgHeight = 1.0f;
    		uniform int _Count = 0;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                float heat = 0.0f;
                float dist;
			    float2 vec;
                
                float2 inv_aspect = float2(_ImgWidth / _ImgHeight, 1);
                float2 normalization = float2(1.0f / _ImgWidth, 1.0f / _ImgHeight);

                for (int i = 0; i < _Count; ++i)
                {
                 	vec = IN.texcoord - (_Points[i].xy * normalization);
                    vec = vec * inv_aspect;

                	//dist = sqrt(vec.x * vec.x + vec.y * vec.y);
               		//heat += (1.0f - saturate(dist * _InvRadius)) * _Intensity * _Points[i].z;

                    //dist = vec.x * vec.x + vec.y * vec.y;
                    //heat += saturate(exp(-dist / (1e-15f + _Points[i].z * _Intensity)));

                    dist = vec.x * vec.x + vec.y * vec.y;
                    heat += saturate(_Points[i].z * _Intensity * exp(-dist / _Radius));
                }
			    return tex2D(_HeatTex, float2(saturate(heat), 0.5f));
                //return float4(1.0f,0.0f,0.0f,1.0f);
            }
        ENDCG
        }
    }
}