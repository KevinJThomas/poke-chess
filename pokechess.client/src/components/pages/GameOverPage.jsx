import Header from "../Header";
import Card from "../Card";
import Button from "../ButtonPage";

export default function GameOverPage({ winner }) {
  function playAgain() {
    window.location.href = "/";
  }
  return (
    <div className="flex h-screen flex-col items-center justify-center bg-indigo-900">
      <Header>{winner} wins!</Header>
      <Card>
        {/* <h3 className="text-base font-semibold text-gray-900">Play again?</h3> */}
        {/* <form className="mt-5 sm:flex sm:items-center">
          <div className="w-full sm:max-w-xs"></div> */}
        <Button fullWidth onClick={playAgain}>
          Play again?
        </Button>
        {/* </form> */}
      </Card>
    </div>
  );
}
