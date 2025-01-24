export default function Tier({ tier }) {
  return (
    <div className="absolute top-0 left-0 flex h-6 w-6 items-center justify-center bg-purple-400">
      <span className="text-sm">{tier}</span>
    </div>
  );
}
