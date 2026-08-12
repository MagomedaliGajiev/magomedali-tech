import Todo from "@/shared/components/todo";

export default function TodoPage() {
  let data = fetch("http://localhost:8001/lessons");

  return (
    <div className="mx-auto w-full max-w-3xl rounded-xl bg-card p-5 ring-1 ring-white/5 sm:p-8">
      <Todo />
    </div>
  );
}
