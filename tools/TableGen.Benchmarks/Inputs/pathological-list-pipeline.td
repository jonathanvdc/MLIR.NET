class IntToString<int value> {
  string ret = !cast<string>(value);
}

class Pipeline<int count> {
  list<int> numbers = !range(0, count);
  list<int> evens = !filter(iter, numbers, !eq(!and(iter, 1), 0));
  list<int> shifted = !foreach(iter, evens, !add(iter, 3));
  int sum = !foldl(0, shifted, acc, cur, !add(acc, cur));
  string joined = !interleave(!foreach(iter, shifted, IntToString<iter>.ret), ",");
}

def Example : Pipeline<256>;
