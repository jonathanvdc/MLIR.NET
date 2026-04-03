class C<int x> {
  int Y = x;
  int Yplus1 = !add(Y, 1);
  int xplus1 = !add(x, 1);
}

let Y = 10 in def Example : C<5>;
