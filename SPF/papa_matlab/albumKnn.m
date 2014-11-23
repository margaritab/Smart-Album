function [ decision ] = albumKnn( userWorkSpace, k )

fileToLearn = strcat(userWorkSpace,'\\Learn.txt');
fileToDecide = strcat(userWorkSpace,'\\Decide.txt');
%profilePath = strcat(userWorkSpace,'\\profilerbf.mat');
%profilePath = strcat(userWorkSpace,'\\profile');
%profilePath = strcat(profilePath, kernel);
%profilePath = strcat(profilePath, '.mat');
resultFilePath = strcat(userWorkSpace,'\\Result.txt');


%load(profilePath,'svmStruct');

M = csvread(fileToLearn);
MLearn = M(1:end,1:15);
group  = M(1:end, 16);


MToDecide = csvread(fileToDecide);
MToDecide = MToDecide(1:1, 1:15);

group     = normr(group);
MLearn     = normr(MLearn);
MToDecide  = normr(MToDecide);
%MLearn    = normalizeMatrix(svmStruct,MLearn);
%MToDecide = normalizeMatrix(svmStruct,MToDecide);

%mdl = ClassificationKNN.fit(MLearn, group, 'NumNeighbors', k);
%[decision, confidence] = predict(mdl, MToDecide);
decision = knnclassify(MToDecide, MLearn, group, k);
resultFile = fopen(resultFilePath, 'w');
fprintf(resultFile, '%d', decision);
%fprintf(resultFile, '%d', confidence);
fclose(resultFile);

end

function [ result ] = normalizeMatrix(svmStruct, matrix)
  for c = 1:size(matrix, 2)
    matrix(:,c) = svmStruct.ScaleData.scaleFactor(c) * ...
    (matrix(:,c) +  svmStruct.ScaleData.shift(c));
  end
  result = matrix;
end