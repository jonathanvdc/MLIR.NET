class Chain0<int width> {
  int Width = width;
  int Score0 = !add(Width, 1);
}

class Chain1<int width> : Chain0<width> {
  int Score1 = !add(Score0, 1);
}

class Chain2<int width> : Chain1<width> {
  int Score2 = !add(Score1, 1);
}

class Chain3<int width> : Chain2<width> {
  int Score3 = !add(Score2, 1);
}

class Chain4<int width> : Chain3<width> {
  int Score4 = !add(Score3, 1);
}

class Chain5<int width> : Chain4<width> {
  int Score5 = !add(Score4, 1);
}

class Chain6<int width> : Chain5<width> {
  int Score6 = !add(Score5, 1);
}

class Chain7<int width> : Chain6<width> {
  int Score7 = !add(Score6, 1);
}

class Chain8<int width> : Chain7<width> {
  int Score8 = !add(Score7, 1);
}

class Chain9<int width> : Chain8<width> {
  int Score9 = !add(Score8, 1);
}

class Chain10<int width> : Chain9<width> {
  int Score10 = !add(Score9, 1);
}

class Chain11<int width> : Chain10<width> {
  int Score11 = !add(Score10, 1);
}

class Chain12<int width> : Chain11<width> {
  int Score12 = !add(Score11, 1);
}

class Chain13<int width> : Chain12<width> {
  int Score13 = !add(Score12, 1);
}

class Chain14<int width> : Chain13<width> {
  int Score14 = !add(Score13, 1);
}

class Chain15<int width> : Chain14<width> {
  int Score15 = !add(Score14, 1);
}

class Chain16<int width> : Chain15<width> {
  int Score16 = !add(Score15, 1);
}

class Chain17<int width> : Chain16<width> {
  int Score17 = !add(Score16, 1);
}

class Chain18<int width> : Chain17<width> {
  int Score18 = !add(Score17, 1);
}

class Chain19<int width> : Chain18<width> {
  int Score19 = !add(Score18, 1);
}

let Width = 128 in def Example : Chain19<4>;
