export default function Health({ health }) {
  return (
    <div className="absolute right-0 bottom-0 flex h-6 w-6 items-center justify-center bg-red-400">
      <span className="text-sm">{health}</span>
    </div>
  );
}
