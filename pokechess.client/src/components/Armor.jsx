export default function Armor({ armor }) {
  return (
    <div className="absolute bottom-0 left-0 flex h-6 w-6 items-center justify-center rounded-full bg-gray-400">
      <span className="text-sm">{armor}</span>
    </div>
  );
}
