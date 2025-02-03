import Pokemon from "./Pokemon";

export default function OpponentPokemon({ player }) {
  return (
    <div className="row">
      {player.board.map((pokemon) => (
        <Pokemon key={pokemon.id} {...pokemon} location="board" />
      ))}
    </div>
  );
}
