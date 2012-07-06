﻿﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Gaia.Core;
using Gaia.Resources;
using Gaia.Voxels;
using Gaia.Input;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;

namespace Gaia.SceneGraph.GameEntities
{
    
    public class Lsystem : Entity
    {
        /*
         * CONSTANTS 
         **/
        const int DEFAULT_ITERATIONS = 3;
        const float DEFAULT_FORWARD_LENGTH = 5.0f;
        const float DEFAULT_INIT_WIDTH = 1.0f;
        const float DEFAULT_SPHERE_RADIUS = 0.25f;
        const float DEFAULT_TURN_VALUE = 35.0f;
        const float DEFAULT_VARIATION = 35.0f;

        const char SYMBOL_FORWARD_F = 'F';
        const char SYMBOL_FORWARD_G = 'G';
        const char SYMBOL_FORWARD_T = 'T';
        const char SYMBOL_FORWARD_NO_DRAW_F = 'f';
        const char SYMBOL_FORWARD_NO_DRAW_G = 'g';
        const char SYMBOL_PITCH_DOWN = '&';
        const char SYMBOL_PITCH_UP = '%';
        const char SYMBOL_POP_MATRIX = ']';
        const char SYMBOL_PUSH_MATRIX = '[';
        const char SYMBOL_ROLL_LEFT = '/';
        const char SYMBOL_ROLL_RIGHT = '\\';
        const char SYMBOL_CLUSTER = '@';
        const char SYMBOL_TURN_AROUND = '|';
        const char SYMBOL_TURN_LEFT = '+';
        const char SYMBOL_TURN_RIGHT = '-';
        const char SYMBOL_TURN_RAND_LEFT = '<';
        const char SYMBOL_TURN_RAND_RIGHT = '>';

        public struct Point3f
        {
            public float x, y, z;
        };

        public struct ReproductionRule
        {
            public char from;
            public string to;
        };

        string axiom;
        bool dirty;
        int iterations;
        float forwardLength;
        float initWidth;
        Stack<Matrix> modelViewStack;
        Stack<Matrix> rotationStack, translationStack;
        static Vector3 yDirection = new Vector3(0.0f, 1.0f, 0.0f);
        Vector3 transDirection = yDirection;
        List<ReproductionRule> rules;
        string result;
        float sphereRadius;
        float turnValue;
        float variation;
        float wRate;
        float lRate;
        Stack<float> widths;
        Stack<float> lengths;
        //Random rand = new Random();
        Vector3 initPosition;
        Vector3 maxPos, minPos;

        List<Matrix> cylinderTransforms = new List<Matrix>();
        List<Matrix> leafTransforms = new List<Matrix>();
        

        // Debugging variables:
        int lineCount = 1;

        public Lsystem()
        {
            this.axiom = "";
            this.iterations = DEFAULT_ITERATIONS;
            this.forwardLength = DEFAULT_FORWARD_LENGTH;
            this.initWidth = DEFAULT_INIT_WIDTH;
            this.turnValue = DEFAULT_TURN_VALUE;
            this.sphereRadius = DEFAULT_SPHERE_RADIUS;
            this.variation = DEFAULT_VARIATION;
            this.dirty = true;

            modelViewStack = new Stack<Matrix>();
            rotationStack = new Stack<Matrix>();
            translationStack = new Stack<Matrix>();
            rules = new List<ReproductionRule>();

            wRate = 0.8f;
            widths = new Stack<float>();
            widths.Push(initWidth);  // Add initial width

            lRate = 0.9f;
            lengths = new Stack<float>();
            lengths.Push(forwardLength);  // Add initial width
        }

        public Lsystem(string axiom,
                        List<ReproductionRule> rules,
                        int iterations,
                        float forwardLength,
                        float initWidth,
                        float turnValue,
                        float sphereRadius,
                        float variation)
        {
            this.axiom = axiom;
            this.iterations = iterations;
            this.forwardLength = forwardLength;
            this.initWidth = initWidth;
            this.turnValue = turnValue;
            this.sphereRadius = sphereRadius;
            this.variation = variation;
            this.dirty = true;

            modelViewStack = new Stack<Matrix>();
            rotationStack = new Stack<Matrix>();
            translationStack = new Stack<Matrix>();
            rules = new List<ReproductionRule>();

            wRate = 0.8f;
            widths = new Stack<float>();
            widths.Push(initWidth);

            lRate = 0.9f;
            lengths = new Stack<float>();
            lengths.Push(forwardLength);  // Add initial width
        }

        public void Destroy()
        {
        }

        /*
         * PUBLIC MEMBER FUNCTIONS 
         **/
        public void addRule(ReproductionRule r)
        {
            rules.Add(r);
        }

        public string getResult()
        {
            return result;
        }

        public List<RenderElement> generateGeometry(Vector3 position, int varyTreeNum)
        {
            cylinderTransforms = new List<Matrix>();
            leafTransforms = new List<Matrix>();
            Vector3 offset = Vector3.Zero;
            offset.Y = -2.0f * forwardLength;
            initPosition = position + offset;
            maxPos = initPosition;
            minPos = initPosition;

            RenderElement cylinderMesh = new RenderElement();
            cylinderMesh.VertexBuffer = GFXPrimitives.CylinderGeometry.GetVertexBufferInstanced();
            cylinderMesh.IndexBuffer = GFXPrimitives.CylinderGeometry.GetIndexBufferInstanced();
            cylinderMesh.StartVertex = 0;
            cylinderMesh.VertexDec = GFXVertexDeclarations.PNTTIDec;
            cylinderMesh.VertexStride = VertexPNTTI.SizeInBytes;
            cylinderMesh.VertexCount = GFXPrimitives.CylinderGeometry.GetVertexCount();
            cylinderMesh.PrimitiveCount = GFXPrimitives.CylinderGeometry.GetPrimitiveCount();

            if (dirty)
            {
                result = generateResult(axiom, 0);
                dirty = false;
            }

            // For testing:
            //result = "G[+&F][-%F[+&F]]GFF@";

            initRotationStack();
            initTranslationStack();

            for (int i = 0; i < result.Length; i++)
            {

                float v;
                switch (result[i])
                {
                    case SYMBOL_FORWARD_F:
                        drawLine();
                        translate();
                        break;
                    case SYMBOL_FORWARD_G:
                        drawLine();
                        translate();
                        break;
                    case SYMBOL_FORWARD_NO_DRAW_F:
                        translate();
                        break;
                    case SYMBOL_FORWARD_NO_DRAW_G:
                        translate();
                        break;
                    case SYMBOL_PITCH_DOWN:
                        v = vary(turnValue);
                        rotate(v, 0.0f, 1.0f, 0.0f);
                        break;
                    case SYMBOL_PITCH_UP:
                        v = vary(turnValue);
                        rotate(-v, 0.0f, 1.0f, 0.0f);
                        break;
                    case SYMBOL_POP_MATRIX:
                        popMatrix();
                        forwardLength = forwardLength / 0.7f;
                        break;
                    case SYMBOL_PUSH_MATRIX:
                        pushMatrix();
                        forwardLength = forwardLength * 0.7f;
                        break;
                    case SYMBOL_ROLL_LEFT:
                        rotate(turnValue, 1.0f, 0.0f, 0.0f);
                        break;
                    case SYMBOL_ROLL_RIGHT:
                        rotate(-turnValue, 1.0f, 0.0f, 0.0f);
                        break;
                    case SYMBOL_CLUSTER:
                        drawLeafCluster(sphereRadius);
                        break;
                    case SYMBOL_TURN_AROUND:
                        rotate(180.0f, 0.0f, 0.0f, 0.0f);
                        break;
                    case SYMBOL_TURN_LEFT:
                        rotate(-turnValue, 0.0f, 0.0f, 1.0f);
                        break;
                    case SYMBOL_TURN_RIGHT:
                        rotate(turnValue, 0.0f, 0.0f, 1.0f);
                        break;
                    case SYMBOL_TURN_RAND_LEFT:
                        v = vary(turnValue + (lineCount*40 % 360));
                     //   float leftRotAngle = (1 + lineCount);
                        
                        rotate(-v, 0.0f, 1.0f, 0.0f);
                        break;
                    case SYMBOL_TURN_RAND_RIGHT:
                        v = vary(turnValue + (lineCount * 40 % 360));
                     //   float rightRotAngle = (1+ lineCount) * 10;
                        rotate(v, 0.0f, 1.0f, 0.0f);
                        break;
                }

            }

            RenderElement leaves = new RenderElement();
            leaves.StartVertex = 0;
            leaves.VertexCount = 4;
            leaves.PrimitiveCount = 4;
            leaves.VertexDec = GFXVertexDeclarations.PTIDec;
            leaves.VertexStride = VertexPTI.SizeInBytes;
            leaves.VertexBuffer = GFXPrimitives.Quad.GetInstanceVertexBuffer();
            leaves.IndexBuffer = GFXPrimitives.Quad.GetInstanceIndexBufferDoubleSided();
            leaves.Transform = leafTransforms.ToArray();

            cylinderMesh.Transform = cylinderTransforms.ToArray();
            List<RenderElement> elements = new List<RenderElement>();
            elements.Add(cylinderMesh);
            elements.Add(leaves);

            return elements;
        }

        // Must be called after generate geometry to get an accurate bounding box
        public BoundingBox getBoundingBox()
        {
            BoundingBox bounds = new BoundingBox();
            bounds.Max = maxPos;
            bounds.Min = minPos;
            return bounds;
        }

        public void setAxiom(string axiom)
        {
            this.axiom = axiom;
            dirty = true;
        }

        public void setForwardLength(float forwardLength)
        {
            this.forwardLength = forwardLength;

            lengths.Clear();
            lengths = new Stack<float>();
            lengths.Push(forwardLength);  // Add initial length
        }

        public void setWidth(float width)
        {
            widths.Pop();
            widths.Push(width);
        }

        public void setIterations(int iterations)
        {
            this.iterations = iterations;
            dirty = true;
        }

        public void setSphereRadius(float sphereRadius)
        {
            this.sphereRadius = sphereRadius;
        }

        public void setTurnValue(float turnValue)
        {
            this.turnValue = turnValue;
        }

        public void setVariation(float variation)
        {
            this.variation = variation;
        }

        public void setWidthRate(float rate)
        {
            this.wRate = rate;
        }

        public void setLengthRate(float rate)
        {
            this.lRate = rate;
        }

        /****************************
         * PRIVATE MEMBER FUNCTIONS *
         ****************************/

        double getNoise()
        {
            return RandomHelper.RandomGen.NextDouble();
        }

        void drawLine()
        {
            float length = lengths.Pop() * lRate;
            lengths.Push(length);
            float width = widths.Pop() * wRate;
            widths.Push(width);
            Vector3 scaleWidth = new Vector3(width, 1, width);
            Vector3 scaleLength = new Vector3(1, length, 1);
            Matrix currTransform = Matrix.CreateScale(scaleWidth * scaleLength) * rotationStack.Peek() * translationStack.Peek();
            cylinderTransforms.Add(currTransform);
        }

        void drawLeafCluster(float scaleSize)
        {
            Matrix transform = Matrix.CreateScale(scaleSize) * rotationStack.Peek() * translationStack.Peek();
            leafTransforms.Add(transform);
        }

        void eraseStack()
        {
            while (rotationStack.Count != 0)
            {
                Matrix top = rotationStack.Pop();
                // erase top
            }
            while (translationStack.Count != 0)
            {
                Matrix top = translationStack.Pop();
                // erase top
            }
        }

        string generateResult(string str, int count)
        {
            for (int i = 0; i < str.Length; i++)
            {
                for (int j = 0; j < rules.Count; j++)
                {
                    if (str[i] == rules[j].from)
                    {
                        if (i == str.Length - 1)
                        {
                            str = str.Substring(0, i) + rules[j].to;
                        }
                        else
                        {
                            str = str.Substring(0, i) + rules[j].to + str.Substring(i + 1, str.Length - i - 1);
                        }
                        i += rules[j].to.Length;
                        break;
                    }
                }
            }

            if (count < iterations) return generateResult(str, count + 1);
            else return str;
        }

        void initRotationStack()
        {
            rotationStack.Clear();
            Matrix worldMatrix = Transformation.GetTransform();

            rotationStack.Push(worldMatrix);

            transDirection = Vector3.Transform(yDirection, worldMatrix);
            transDirection.Normalize();
        }

        void initTranslationStack()
        {
            translationStack.Clear();
            Matrix initPositionMatrix = Matrix.Identity;
            Matrix.CreateTranslation(ref initPosition, out initPositionMatrix);
            translationStack.Push(initPositionMatrix);
        }

        void popMatrix()
        {
            if (rotationStack.Count > 0)
            {
                Matrix top = rotationStack.Pop();
                // delete top

            }

            if (rotationStack.Count == 0)
            {
                initRotationStack();
            }
            else
            {
                transDirection = Vector3.Transform(yDirection, rotationStack.Peek());
                transDirection.Normalize();
            }

            if (translationStack.Count > 0)
            {
                Matrix top = translationStack.Pop();
                // delete top
            }

            if (translationStack.Count == 0)
            {
                initTranslationStack();
            }

            widths.Pop();
            lengths.Pop();
        }

        void pushMatrix()
        {
            rotationStack.Push(rotationStack.Peek());
            translationStack.Push(translationStack.Peek());
            widths.Push(widths.Peek());
            lengths.Push(lengths.Peek());
        }

        void rotate(float r, float rx, float ry, float rz)
        {
            Vector3 axis;
            axis.X = rx;
            axis.Y = ry;
            axis.Z = rz;

            float radians = MathHelper.ToRadians(r);
            Matrix rotatedTop = rotationStack.Pop() * Matrix.CreateFromAxisAngle(axis, radians);
            rotationStack.Push(rotatedTop);

            transDirection = Vector3.Transform(yDirection, rotatedTop);
            transDirection.Normalize();
        }

        void translate()
        {
            float distance = lengths.Peek();
            Vector3 axis;
            axis.X = transDirection.X * distance;
            axis.Y = transDirection.Y * distance;
            axis.Z = transDirection.Z * distance;
            Matrix translatedTop = translationStack.Pop() * Matrix.CreateTranslation(axis);
            translationStack.Push(translatedTop);

            Vector3 newPosition = Vector3.Transform(Vector3.Zero, translatedTop);
            minPos = Vector3.Min(newPosition, minPos);
            maxPos = Vector3.Max(newPosition, maxPos);

        }

        float vary(float v)
        {
            float n = (float)getNoise();
            n -= 0.5f;
            float var = (variation * n);
            return v + var;
        }

    }
}