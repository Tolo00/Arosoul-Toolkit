using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arosoul.Essentials {

    [System.Serializable]
    public class Array2DContainer<T> {
        public T[] arrayData;

        public int width;
        public int height;

        public int Width => width;
        public int Height => height;

        public Array2DContainer(int width, int height) {
            arrayData = new T[width * height];
            this.width = width;
            this.height = height;
        }

        public T this[int x, int y] {
            get {
                if (x >= width || x < 0 || y >= height || y < 0)
                    throw new IndexOutOfRangeException();

                return arrayData[x + y * width];
            }
            set {
                if (x >= width || x < 0 || y >= height || y < 0)
                    throw new IndexOutOfRangeException();

                arrayData[x + y * width] = value;
            }
        }

        public int Length {
            get { return arrayData.Length; }
        }

        public Vector2Int GetPosByValue(T value) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (EqualityComparer<T>.Default.Equals(GetValueByPos(x, y), value)) {
                        return new Vector2Int(x, y);
                    }
                }
            }

            throw new Exception($"Value '{value}' could not be found in 2D Array!");
        }

        public T GetValueByVector(Vector2 vector) {
            return GetValueByPos(Mathf.FloorToInt(vector.x), Mathf.FloorToInt(vector.y));
        }

        private T GetValueByPos(int x, int y) {
            return arrayData[x + y * width];
        }

        public bool IsPosValid(Vector2Int pos) {
            if (pos.x < 0 || pos.x >= width) return false;
            if (pos.y < 0 || pos.y >= height) return false;
            return true;
        }
    }

}
