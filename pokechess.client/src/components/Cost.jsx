export default function Cost({ cost }) {
  return (
    <div className="absolute top-0 right-0 flex h-6 w-6 items-center justify-center rounded-full bg-amber-400">
      <span className="text-sm">{cost}</span>
    </div>
  );
}
