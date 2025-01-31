export default function Attack({ attack }) {
  return (
    <div className="absolute bottom-0 left-0 z-5 flex h-6 w-6 items-center justify-center rounded-full bg-amber-400">
      <span className="text-sm">{attack}</span>
    </div>
  );
}
