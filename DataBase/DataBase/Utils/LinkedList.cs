
using System.Collections;


namespace DataBase.Utils
{
    public class LinkedList<T> : IEnumerable<T>
    {
        private Node<T>? head;
        private Node<T>? tail;

        public Node<T>? Head => head;

        public void Add(T value)
        {
            var node = new Node<T>(value);
            if (head == null)
                head = tail = node;
            else
            {
                tail.Next = node;
                tail = node;
            }
        }

        public int Count()
        {
            int count = 0;
            var current = head;
            while (current != null)
            {
                count++;
                current = current.Next;
            }
            return count;
        }

        public T GetAt(int index)
        {
            var current = head;
            int i = 0;
            while (current != null)
            {
                if (i == index) return current.Value;
                current = current.Next;
                i++;
            }
            throw new IndexOutOfRangeException();
        }

        public bool IsEmpty() => head == null;

       

        public IEnumerator<T> GetEnumerator()
        {
            var current = head;
            while (current != null)
            {
                yield return current.Value;
                current = current.Next;
            }
        }

        public bool Remove(T value)
        {
            if (head == null)
                return false;


            if (head.Value != null && head.Value.Equals(value))
            {
                head = head.Next;
                if (head == null)
                    tail = null;
                return true;
            }

            var current = head;
            while (current.Next != null)
            {
                if (head.Value != null && head.Value.Equals(value))
                {

                    current.Next = current.Next.Next;


                    if (current.Next == null)
                        tail = current;

                    return true;
                }
                current = current.Next;
            }

            return false;
        }

        public bool RemoveAt(int index)
        {
            if (index < 0 || head == null)
                return false;

            if (index == 0)
            {
                head = head.Next;
                if (head == null)
                    tail = null;
                return true;
            }

            var current = head;
            int i = 0;
            while (current.Next != null)
            {
                if (i == index - 1)
                {
                    current.Next = current.Next.Next;
                    if (current.Next == null)
                        tail = current;
                    return true;
                }
                current = current.Next;
                i++;
            }

            return false; 
        }



        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}


