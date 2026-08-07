import Counter from "./counter";

export default function Home() {
  return (
    <div className="flex flex-1 items-center justify-center bg-background px-6 py-12 font-sans">
      <main className="w-full max-w-md rounded-lg border bg-card p-8 shadow-sm">
        <Counter />
      </main>
    </div>
  );
}
